import re
from pathlib import Path
from unityparser import UnityDocument
import networkx as nx
import matplotlib.pyplot as plt
import time
import functools
import itertools as it
from interactable import Interactable
from interactor import Interactor
from prefab import Prefab, PrefabType
from interaction import Interaction, InteractionType


def log_execution_time(func):
    """
    Decorator to log the execution time of a function
    """

    @functools.wraps(func)
    def wrapper(*args, **kwargs):
        start_time = time.time()
        result = func(*args, **kwargs)
        end_time = time.time()
        print(f"Execution time: {end_time - start_time} seconds")
        return result

    return wrapper


def cache_result(func):
    """
    Decorator to cache the result of a function.
    Creates a cache dictionary for each decorated function to store results.
    The cache is stored as an instance attribute on the class.
    Args:
        func: The function to be decorated
    Returns:
        wrapper: The wrapped function that implements caching
    """

    @functools.wraps(func)
    def wrapper(self, *args, **kwargs):
        # Create unique cache attribute name for this function
        cache_attr = f"_{func.__name__}_cache"
        # Create unique key based on args and kwargs
        cache_key = str(args) + str(kwargs)
        # Initialize cache dict if it doesn't exist
        if not hasattr(self, cache_attr):
            setattr(self, cache_attr, {})
        # Use *getattr* instead of *setattr* to add a new key to the cache
        cache = getattr(self, cache_attr)
        # Return cached result if it exists, otherwise compute and cache
        if cache_key not in cache:
            cache[cache_key] = func(self, *args, **kwargs)
        return cache[cache_key]

    return wrapper


class InteractionGraph:
    def __init__(self, root, sut):
        self.root = root
        self.asset_path = [
            self.root / "Assets",
            self.root / "Library"
        ]
        self.sut = sut
        self.scene_doc = UnityDocument.load_yaml(self.sut)
        self.custom_script_path = self.root / "Assets/VRTemplateAssets/Scripts"

    @cache_result
    def get_assets(self, suffix="*.meta"):
        """
        Get all assets based on the asset paths, default to .meta files
        """
        assets = set()
        for path in self.asset_path:
            for asset in path.rglob(suffix):
                assets.add(asset)
        return assets

    @cache_result
    def get_asset_prefabs(self):
        """
        Get all the prefabs in the asset path
        """
        return self.get_assets("*.prefab.meta")

    def get_asset_name_by_guid(self, guid):
        """
        Get the asset name by the guid
        """
        for asset in self.get_assets():
            found_guid = self.get_file_guid(asset)
            if found_guid == guid:
                return asset.stem  # Get the file name without the suffix
        return None

    @cache_result
    def get_file_guid(self, file_name):
        """
        Get the guid of the file
        """
        with open(file_name, 'r', encoding='utf-8') as f:
            content = f.read()
            guid_match = re.search(r'guid: (\w+)', content)
            if guid_match:
                found_guid = guid_match.group(1)
                return found_guid
        return None

    def get_entry_by_anchor(self, anchor):
        """
        Example:
        Information from .unity file
        ...
        --- !u!1660057539 &9223372036854775807
        SceneRoots:
        ...
        entry.anchor == 9223372036854775807
        entry.__class__.__name__ == SceneRoots
        """
        for entry in self.scene_doc.entries:
            if entry.anchor == anchor:
                return entry
        return None

    @cache_result
    def _get_asset_prefab_guids(self):
        """
        Get the guids of all prefabs in the asset path
        """
        asset_prefab_guids = {}
        for meta_file in self.get_asset_prefabs():
            guid = self.get_file_guid(meta_file)
            asset_prefab_guids[guid] = meta_file
        return asset_prefab_guids

    @cache_result
    def _get_prefab_objects(self, doc):
        """
        Get all prefabs within a Unity doc file
        """
        # Record the prefab file names with the guids as keys
        asset_prefab_guids = self._get_asset_prefab_guids()
        # for meta_file in self.get_asset_prefabs():
        #     guid = self.get_file_guid(meta_file)
        #     asset_prefab_guids[guid] = meta_file
        prefabs = set()
        scene_prefab_instances = doc.filter(
            class_names=("PrefabInstance",), attributes=("m_SourcePrefab",))
        for instance in scene_prefab_instances:
            prefab_guid = instance.m_SourcePrefab.get("guid")
            if prefab_guid in asset_prefab_guids:
                prefab_name = asset_prefab_guids[prefab_guid].stem.replace(
                    ".prefab", "")
                prefab_path = asset_prefab_guids[prefab_guid].parent / \
                              asset_prefab_guids[prefab_guid].stem
                interaction_layer = self._get_interaction_layer(
                    instance=instance, )
                # TODO: could set the prefab type according to the input doc path
                prefab = Prefab(name=prefab_name,
                                guid=prefab_guid,
                                file=prefab_path,
                                type=PrefabType.SCENE,
                                interaction_layer=interaction_layer)
                prefabs.add(prefab)
        return prefabs

    def _get_prefab_name(self, obj_id):
        """Helper to get prefab name from object ID"""
        if entry := self.get_entry_by_anchor(obj_id):
            if prefab_id := entry.m_PrefabInstance.get("fileID"):
                if prefab_entry := self.get_entry_by_anchor(prefab_id):
                    for mod in prefab_entry.m_Modification["m_Modifications"]:
                        if mod.get("propertyPath") == "m_Name":
                            return mod.get("value")
        return None

    def get_prefabs_source_from_scene(self):
        """
        Get the prefabs in the scene under test
        """
        return self._get_prefab_objects(self.scene_doc)

    def _get_interaction_layer(self, instance=None, prefab_source=None, obj_id=None):
        """
        Get the interaction layer of the prefab instance
        """
        # If there are modifications related to the interaction layer is done within the scene
        if instance:
            for mod in instance.m_Modification["m_Modifications"]:
                if mod.get("propertyPath") == "m_InteractionLayers.m_Bits":
                    return mod.get("value")
        elif prefab_source:
            # TODO: If there are no modifications related to the interaction layer, check the prefab source
            pass
        elif obj_id:
            if entry := self.get_entry_by_anchor(obj_id):
                if prefab_id := entry.m_PrefabInstance.get("fileID"):
                    if prefab_entry := self.get_entry_by_anchor(prefab_id):
                        for mod in prefab_entry.m_Modification["m_Modifications"]:
                            if mod.get("propertyPath") == "m_InteractionLayers.m_Bits":
                                return mod.get("value")
        return -1  # Assume the default interaction layer is -1

    @cache_result
    def is_custom_xr_interaction(self, cs_file_path):
        """
        Check if a C# script inherits from XRBaseInteractable or XRGrabInteractable
        and located in the custom script path. Returns the matching classes if found.
        """
        # First check if file is in custom script path
        if not str(cs_file_path).startswith(str(self.custom_script_path)):
            return False
        target_classes = {"XRBaseInteractable", "XRGrabInteractable"}
        try:
            with open(cs_file_path, 'r', encoding='utf-8') as f:
                content = f.read()
                class_pattern = r'class\s+\w+\s*:\s*([\w,\s]+)'
                match = re.search(class_pattern, content)
                if match:
                    inheritance = match.group(1)
                    inherited_classes = {cls.strip()
                                         for cls in inheritance.split(',')}
                    # Return the matching classes if any exist
                    matching_classes = inherited_classes & target_classes
                    return matching_classes if matching_classes else False
        except Exception as e:
            print(f"Error reading file {cs_file_path}: {e}")
        return False

    @cache_result
    def get_interaction_types(self):
        """
        Get the scripts that have "Interactable" or "Interactor" in the name
        Based on .meta files and record the guid of the script
        Returns dictionary with:
            - guid: file guid
            - file: path to cs file
            - type: interaction type {activate, select, activate* (custom activate), select* (custom select)}
        """
        scripts = set()
        for asset in self.get_assets("*.cs.*"):
            file_name = asset.stem  # Get the file name without the suffix
            cs_file = asset.parent / asset.stem
            interaction_type = set()
            # Skip deprecated and affordance files
            if "deprecated" in file_name or "Affordance" in file_name:
                continue
            if "Interactable" in file_name or "Interactor" in file_name:
                if guid := self.get_file_guid(asset):
                    if "XRBaseInteractable" in file_name:
                        interaction_type.add("activate")
                    elif "XRGrabInteractable" in file_name:
                        interaction_type.add("select")
                    else:
                        # TODO: check if it is custom interaction
                        interaction_type.add("CUSTOM")
                    interaction = Interaction(name=file_name,
                                              file=cs_file,
                                              guid=guid,
                                              interaction_type=interaction_type)
                    scripts.add(interaction)
            # Check custom XR interactions
            elif custom_type := self.is_custom_xr_interaction(cs_file):
                if guid := self.get_file_guid(asset):
                    interaction_type.add(
                        "activate*" if "XRBaseInteractable" in custom_type else "select*")
                    interaction = Interaction(name=file_name,
                                              file=cs_file,
                                              guid=guid,
                                              interaction_type=interaction_type)
                    scripts.add(interaction)
        return scripts

    @staticmethod
    def _has_precondition(prefab_doc):
        """
        Check if select interactions also supported activate interactions (m_Activated in MonoBehaviour)
        """
        for entry in prefab_doc.filter(class_names=("MonoBehaviour",), attributes=("m_Activated",)):
            if calls := entry.m_Activated.get("m_PersistentCalls", {}).get("m_Calls"):
                if calls:  # If m_Calls is not empty
                    return True
        return False

    def _process_prefab_interactions(self, prefab):
        """
        Helper method to process scripts in a prefab and categorize them
        Returns: Dictionary with sets of interactables and interactors
        """
        prefab_doc = UnityDocument.load_yaml(prefab.file)
        results = {'interactables': set(), 'interactors': set()}
        script_guids = {script.m_Script.get("guid") for script in
                        prefab_doc.filter(class_names=("MonoBehaviour",), attributes=("m_Script",))}
        for interaction in self.get_interaction_types():
            if interaction.guid not in script_guids:
                continue
            if "Interactor" in interaction.name:
                interactor = Interactor(name=prefab.name,
                                        script=interaction.file,
                                        interaction_layer=prefab.interaction_layer)
                results["interactors"].add(interactor)
            elif "Interactable" in interaction.name or self.is_custom_xr_interaction(interaction.file):
                if self._has_precondition(prefab_doc):
                    interaction.interaction_type.add("activate")
                interactable = Interactable(name=prefab.name,
                                            script=interaction.file,
                                            interaction_type=interaction.interaction_type,
                                            interaction_layer=prefab.interaction_layer)
                results["interactables"].add(interactable)
            break
        return results

    def _get_nested_prefab(self, prefab):
        """
        Helper method to get paths of nested prefabs
        Returns: List of Path objects for nested prefabs
        """
        prefab_doc = UnityDocument.load_yaml(prefab.file)
        return self._get_prefab_objects(prefab_doc)

    @cache_result
    def get_interactive_prefabs(self):
        """
        Get prefab instances from the scene that are either Interactable or Interactor
        Returns: Dictionary with sets of interactables and interactors
        """

        def find_interactives_in_prefab(prefab, processed):
            if prefab in processed:
                return {'interactables': set(), 'interactors': set()}
            processed.add(prefab)
            results = {'interactables': set(), 'interactors': set()}
            # Process current prefab's scripts
            current_results = self._process_prefab_interactions(prefab)
            results["interactables"].update(
                current_results["interactables"])
            results["interactors"].update(current_results["interactors"])
            # Process nested prefabs
            for nested_prefab in self._get_nested_prefab(prefab):
                nested_results = find_interactives_in_prefab(
                    nested_prefab, processed)
                results["interactables"].update(
                    nested_results["interactables"])
                results["interactors"].update(
                    nested_results["interactors"])
            return results

        final_results = {'interactables': set(), 'interactors': set()}
        processed_prefabs = set()
        # Process all prefabs from the scene
        for prefab_source in self.get_prefabs_source_from_scene():
            results_tmp = find_interactives_in_prefab(prefab_source, processed_prefabs)
            final_results["interactables"].update(results_tmp["interactables"])
            final_results["interactors"].update(results_tmp["interactors"])
        return final_results

    def get_scene_interactives(self):
        """
        Get the interactables and interactors in the scene under test
        Returns: Two sets of interactable and interactor objects
        """
        results = {'interactables': set(), 'interactors': set()}
        scene_scripts = self.scene_doc.filter(class_names=(
            "MonoBehaviour",), attributes=("m_Script",))
        for script in scene_scripts:
            for interaction in self.get_interaction_types():
                if interaction.guid != script.m_Script.get("guid"):
                    continue
                # Get the file id of the game object linked to the interactive script
                obj_id = script.m_GameObject.get("fileID")
                if "Interactable" in interaction.name or self.is_custom_xr_interaction(interaction.file):
                    interactable = Interactable(
                        name=self._get_prefab_name(obj_id),
                        script=interaction.file,
                        interaction_type=interaction.interaction_type,
                        interaction_layer=self._get_interaction_layer(obj_id=obj_id), )
                    results["interactables"].add(interactable)
                elif "Interactor" in interaction.name:
                    interactor = Interactor(
                        name=self._get_prefab_name(obj_id),
                        script=interaction.file,
                        interaction_layer=self._get_interaction_layer(obj_id=obj_id), )
                    results["interactors"].add(interactor)
        return results

    def get_ui_objects(self):
        """
        Get all ui objects from the scene and prefabs
        """
        default_ui_interaction_type = "activate"  # set the default interaction types for all uis to "activate"
        default_ui_interaction_script = None
        default_ui_interaction_layer = -2 # UI objects do not have interaction layers, set value to -2
        # scene ui objects
        scene_uis = set()
        delegates = self.scene_doc.filter(
            class_names=("MonoBehaviour",), attributes=("m_Delegates",))
        for delegate in delegates:
            obj_id = delegate.m_GameObject.get("fileID")
            ui = Interactable(name=self._get_prefab_name(obj_id),
                              script=default_ui_interaction_script,
                              interaction_type=default_ui_interaction_type,
                              interaction_layer=default_ui_interaction_layer
                              )
            scene_uis.add(ui)

        def has_delegates_in_prefab(prefab, processed):
            """
            Recursive search method to get nested prefab ui objects
            """
            if prefab in processed:
                return False
            processed.add(prefab)
            # check if the prefab contain event triggers
            prefab_doc = UnityDocument.load_yaml(prefab.file)
            if prefab_doc.filter(class_names=("MonoBehaviour",), attributes=("m_Delegates",)):
                return True
            # check nested prefabs
            for nested_prefab in self._get_nested_prefab(prefab):
                if has_delegates_in_prefab(nested_prefab, processed):
                    return True
            return False

        # prefab ui objects
        prefab_uis = set()
        processed_prefabs = set()
        for prefab in self.get_prefabs_source_from_scene():
            if has_delegates_in_prefab(prefab, processed_prefabs):
                ui = Interactable(
                    name=prefab.name,
                    script=default_ui_interaction_script,
                    interaction_type=default_ui_interaction_type,
                    interaction_layer=default_ui_interaction_layer
                )
                prefab_uis.add(ui)
        return scene_uis.union(prefab_uis)

    @log_execution_time
    def get_interactors_interactables(self):
        """
        Categorise scene interactives and interactive prefabs into interactables and interactors
        Returns: Dictionary with two lists - 'interactables' and 'interactors'
        """
        prefab_results = self.get_interactive_prefabs()
        scene_results = self.get_scene_interactives()
        uis = self.get_ui_objects()
        merged_results = {
            'interactors': prefab_results['interactors'].union(scene_results['interactors']),
            'interactables': prefab_results['interactables'].union(scene_results['interactables']).union(uis)
        }
        print(
            f"Interactors: {merged_results['interactors']}, length: {len(merged_results['interactors'])}")
        print(
            f"Interactables: {merged_results['interactables'].difference(uis)}, length: {len(merged_results['interactables'].difference(uis))}")
        print(f"UIs: {uis}, length: {len(uis)}")
        return merged_results

    @log_execution_time
    def build_graph(self):
        G = nx.MultiDiGraph()
        connectionstyles = [f"arc3,rad={r}" for r in it.accumulate([0.15] * 4)]
        colors = {
            'select': 'red',
            'activate': 'blue',
            'select*': 'darkred',
            'activate*': 'darkblue',
            'CUSTOM-TODO': 'purple'
        }
        # Add nodes and edges
        interactives = self.get_interactors_interactables()
        # Get first interactor
        interactor = next(iter(interactives['interactors']))
        G.add_node(interactor)

        edges_by_type = {}
        for interactable, interaction_type in interactives['interactables']:
            G.add_node(interactable)
            for type in interaction_type.split("+"):
                G.add_edge(interactor, interactable, key=type)
                edges_by_type.setdefault(type, []).append(
                    (interactor, interactable))
        # Draw graph
        pos = nx.spring_layout(G)
        nx.draw_networkx_nodes(G, pos)
        nx.draw_networkx_labels(G, pos)
        # Draw edges for each interaction type
        for i, (edge_type, edges) in enumerate(edges_by_type.items()):
            nx.draw_networkx_edges(
                G, pos,
                edgelist=edges,
                edge_color=colors.get(edge_type, 'gray'),
                connectionstyle=connectionstyles[i % len(connectionstyles)]
            )
            edge_labels = {(u, v): edge_type for u, v in edges}
            nx.draw_networkx_edge_labels(G, pos, edge_labels=edge_labels)
        plt.show()

    def test(self):
        # interactive_prefabs = self.get_interactive_prefabs()
        # print([_.name + str(_.interaction_type) for _ in interactive_prefabs['interactables']],
        #       len(interactive_prefabs['interactables']))
        # print([_.name for _ in interactive_prefabs['interactors']],
        #       len(interactive_prefabs['interactors']))

        # scene_interactives = self.get_scene_interactives()
        # print([_.name + str(_.interaction_type) for _ in scene_interactives['interactables']],
        #       len(scene_interactives['interactables']))
        # print([_.name for _ in scene_interactives['interactors']],
        #       len(scene_interactives['interactors']))

        uis = self.get_ui_objects()
        print([_.name for _ in uis], len(uis))

        # self.get_interactors_interactables()
        # self.build_graph()


if __name__ == '__main__':
    # need to set the path of the scene, and also the path of where the prefabs are stored
    project_root = Path("/Users/ruizhengu/Projects/InteractoBot/envs/XRIExample")
    scene_under_test = project_root / "Assets/Scenes/SampleScene.unity"

    graph = InteractionGraph(project_root, scene_under_test)
    graph.test()
    # print(graph.get_asset_name_by_guid("cec1aebf75b74914097378398b58a48e"))
