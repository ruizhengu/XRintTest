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
from prefab import Prefab, Type


def log_execution_time(func):
    '''
    Decorator to log the execution time of a function
    '''
    @functools.wraps(func)
    def wrapper(*args, **kwargs):
        start_time = time.time()
        result = func(*args, **kwargs)
        end_time = time.time()
        print(f"Execution time: {end_time - start_time} seconds")
        return result
    return wrapper


def cache_result(func):
    '''
    Decorator to cache the result of a function.
    Creates a cache dictionary for each decorated function to store results.
    The cache is stored as an instance attribute on the class.
    Args:
        func: The function to be decorated
    Returns:
        wrapper: The wrapped function that implements caching
    '''
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
        '''
        Get all assets based on the asset paths, default to .meta files
        '''
        assets = set()
        for path in self.asset_path:
            for asset in path.rglob(suffix):
                assets.add(asset)
        return assets

    @cache_result
    def get_asset_prefabs(self):
        '''
        Get all the prefabs in the asset path
        '''
        return self.get_assets("*.prefab.meta")

    def get_asset_name_by_guid(self, guid):
        '''
        Get the asset name by the guid
        '''
        for asset in self.get_assets():
            found_guid = self.get_file_guid(asset)
            if found_guid == guid:
                return asset.stem  # Get the file name without the suffix
        return None

    @cache_result
    def get_file_guid(self, file_name):
        '''
        Get the guid of the file
        '''
        with open(file_name, 'r', encoding='utf-8') as f:
            content = f.read()
            guid_match = re.search(r'guid: (\w+)', content)
            if guid_match:
                found_guid = guid_match.group(1)
                return file_name, found_guid
        return None

    def get_entry_by_anchor(self, anchor):
        '''
        Example:
        Information from .unity file
        ...
        --- !u!1660057539 &9223372036854775807
        SceneRoots:
        ...
        entry.anchor == 9223372036854775807
        entry.__class__.__name__ == SceneRoots
        '''
        for entry in self.scene_doc.entries:
            if entry.anchor == anchor:
                return entry
        return None

    def get_prefabs_source_from_scene(self):
        '''
        Get the prefab source according to the prefabs in the scene
        '''
        prefab_guids = set(self.get_scene_prefabs())
        prefab_files = {}
        # Find prefab source files that match scene prefab GUIDs
        for meta_file in self.get_assets("*.prefab.meta"):
            guid = self.get_file_guid(meta_file)
            if guid in prefab_guids:
                prefab_name = meta_file.stem
                prefab_files[prefab_name.replace(".prefab", "")] = meta_file.parent / \
                    prefab_name
        return prefab_files

    def get_prefabs(self):
        '''
        Get the prefabs in the scene under test
        '''
        prefabs = set()
        asset_prefab_guids = {}
        for meta_file in self.get_asset_prefabs():
            file_name, guid = self.get_file_guid(meta_file)
            # Record the file_name with the guid as key
            asset_prefab_guids[guid] = file_name
        # Get the prefab instances within the scene under test
        scene_prefab_instances = self.scene_doc.filter(
            class_names=("PrefabInstance",), attributes=("m_SourcePrefab",))
        for instance in scene_prefab_instances:
            guid = instance.m_SourcePrefab.get("guid")
            if guid in asset_prefab_guids:
                # Use the recorded file_name
                prefab_name = Path(asset_prefab_guids[guid]).stem
                interaction_layer = self.get_interaction_layer(instance)
                prefab = Prefab(name=prefab_name,
                                guid=guid,
                                type=Type.SCENE,
                                interaction_layer=interaction_layer)
                prefabs.add(prefab)
        return prefabs

    def get_interaction_layer(self, instance):
        '''
        Get the interaction layer of the prefab instance
        '''
        # If there are modifications related to the interaction layer
        # **is done within the scene**
        for mod in instance.m_Modification["m_Modifications"]:
            if mod.get("propertyPath") == "m_InteractionLayers.m_Bits":
                return mod.get("value")
        return None

    @cache_result
    def get_scene_prefabs(self):
        '''
        Get all the prefab instances in the scene under test
        '''
        prefab_guids = []
        prefab_instances = self.scene_doc.filter(
            class_names=("PrefabInstance",), attributes=("m_SourcePrefab",))
        for instance in prefab_instances:
            prefab_guids.append(instance.m_SourcePrefab.get("guid"))
        return prefab_guids

    @cache_result
    def is_custom_xr_interaction(self, cs_file_path):
        '''
        Check if a C# script inherits from XRBaseInteractable or XRGrabInteractable
        and located in the custom script path. Returns the matching classes if found.
        '''
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
    def get_interaction_scripts(self):
        '''
        Get the scripts that have "Interactable" or "Interactor" in the name
        Based on .meta files and record the guid of the script
        Returns dictionary with:
            - guid: file guid
            - file: path to cs file
            - type: interaction type {activate, select, activate* (custom activate), select* (custom select)}
        '''
        scripts = {}
        for asset in self.get_assets("*.cs.*"):
            file_name = asset.stem  # Get the file name without the suffix
            cs_file = asset.parent / asset.stem
            # Skip deprecated and affordance files
            if "deprecated" in file_name or "Affordance" in file_name:
                continue
            if "Interactable" in file_name or "Interactor" in file_name:
                if guid := self.get_file_guid(asset):
                    script_data = {
                        "guid": guid,
                        "file": cs_file
                    }
                    # Determine interaction type
                    if "XRBaseInteractable" in file_name:
                        script_data["type"] = "activate"
                    elif "XRGrabInteractable" in file_name:
                        script_data["type"] = "select"
                    else:
                        # TODO: check if it is custom interaction
                        script_data["type"] = "CUSTOM-TODO"
                    scripts[file_name] = script_data
            # Check custom XR interactions
            elif custom_type := self.is_custom_xr_interaction(cs_file):
                if guid := self.get_file_guid(asset):
                    scripts[file_name] = {
                        "guid": guid,
                        "file": cs_file,
                        "type": "activate*" if "XRBaseInteractable" in custom_type else "select*"
                    }
        return scripts

    def _has_precondition(self, prefab_doc):
        '''
        Check if select interactions also supported activate interactions (m_Activated in MonoBehaviour)
        '''
        for entry in prefab_doc.filter(class_names=("MonoBehaviour",), attributes=("m_Activated",)):
            if calls := entry.m_Activated.get("m_PersistentCalls", {}).get("m_Calls"):
                if calls:  # If m_Calls is not empty
                    return True
        return False

    def _process_prefab_interactions(self, prefab_doc, prefab_name):
        '''
        Helper method to process scripts in a prefab and categorize them
        Returns: Dictionary with sets of interactables and interactors
        '''
        results = {'interactables': set(), 'interactors': set()}
        script_guids = {script.m_Script.get("guid") for script in
                        prefab_doc.filter(class_names=("MonoBehaviour",), attributes=("m_Script",))}
        for script_name, data in self.get_interaction_scripts().items():
            if data["guid"] not in script_guids:
                continue
            if "Interactor" in script_name:
                interactor = Interactor(
                    prefab_name, data["file"], data["type"], data["interaction_layer"])
                results["interactors"].add(prefab_name)
            elif "Interactable" in script_name or self.is_custom_xr_interaction(data["file"]):
                interaction_type = data["type"]
                if self._has_precondition(prefab_doc):
                    interaction_type += "+activate"
                results["interactables"].add((prefab_name, interaction_type))
            break
        return results

    def _get_nested_prefab_paths(self, prefab_doc):
        '''
        Helper method to get paths of nested prefabs
        Returns: List of Path objects for nested prefabs
        '''
        nested_paths = []
        for instance in prefab_doc.filter(class_names=("PrefabInstance",), attributes=("m_SourcePrefab",)):
            if source_guid := instance.m_SourcePrefab.get("guid"):
                for meta_file in self.get_assets("*.prefab.meta"):
                    if self.get_file_guid(meta_file) == source_guid:
                        nested_paths.append(meta_file.parent / meta_file.stem)
        return nested_paths

    @cache_result
    def get_interactive_prefabs(self):
        '''
        Get prefab instances from the scene that are either Interactable or Interactor
        Returns: Dictionary with sets of interactables and interactors
        '''
        def find_interactives_in_prefab(prefab_path, processed):
            if prefab_path in processed:
                return {'interactables': set(), 'interactors': set()}
            processed.add(prefab_path)
            results = {'interactables': set(), 'interactors': set()}
            prefab_doc = UnityDocument.load_yaml(prefab_path)
            # Process current prefab's scripts
            current_results = self._process_prefab_scripts(
                prefab_doc, prefab_path.stem)
            results["interactables"].update(
                current_results["interactables"])
            results["interactors"].update(current_results["interactors"])
            # Process nested prefabs
            for nested_path in self._get_nested_prefab_paths(prefab_doc):
                nested_results = find_interactives_in_prefab(
                    nested_path, processed)
                results["interactables"].update(
                    nested_results["interactables"])
                results["interactors"].update(
                    nested_results["interactors"])
            return results
        final_results = {'interactables': set(), 'interactors': set()}
        processed_prefabs = set()
        # Process all prefabs from the scene
        for _, prefab_path in self.get_prefabs_source_from_scene().items():
            results = find_interactives_in_prefab(
                prefab_path, processed_prefabs)
            final_results["interactables"].update(results["interactables"])
            final_results["interactors"].update(results["interactors"])
        return final_results

    def _get_prefab_name(self, obj_id):
        '''Helper to get prefab name from object ID'''
        if entry := self.get_entry_by_anchor(obj_id):
            if prefab_id := entry.m_PrefabInstance.get("fileID"):
                if prefab_entry := self.get_entry_by_anchor(prefab_id):
                    for mod in prefab_entry.m_Modification["m_Modifications"]:
                        if mod.get("propertyPath") == "m_Name":
                            return mod.get("value")
        return None

    def _get_script_mappings(self):
        '''Helper to map script GUIDs to their types'''
        mappings = {'type_map': {}, 'interaction_types': {}}

        for script_name, data in self.get_interaction_scripts().items():
            guid = data["guid"]
            if "Interactable" in script_name or self.is_custom_xr_interaction(data["file"]):
                mappings['type_map'][guid] = "interactables"
                mappings['interaction_types'][guid] = data["type"]
            elif "Interactor" in script_name:
                mappings['type_map'][guid] = "interactors"
        return mappings

    def _collect_interactive_objects(self, scene_scripts, script_mappings):
        '''Helper to collect game objects with interaction scripts'''
        game_objects = {'interactables': {}, 'interactors': {}}

        for script in scene_scripts:
            guid = script.m_Script.get("guid")
            if guid in script_mappings['type_map']:
                obj_type = script_mappings['type_map'][guid]
                obj_id = script.m_GameObject.get("fileID")
                interaction_type = script_mappings['interaction_types'].get(
                    guid)
                game_objects[obj_type][obj_id] = interaction_type

        return game_objects

    def get_scene_interactives(self):
        '''
        Get the interactables and interactors in the scene under test
        Returns: Dictionary with sets of (name, type) tuples for interactables and interactors
        '''
        results = {'interactables': set(), 'interactors': set()}
        # Map script GUIDs to their types and interaction types
        script_mappings = self._get_script_mappings()
        # Get game objects with interaction scripts
        game_objects = self._collect_interactive_objects(
            self.scene_doc.filter(class_names=(
                "MonoBehaviour",), attributes=("m_Script",)),
            script_mappings
        )
        # Add prefab names and types to results
        for obj_type in ('interactables', 'interactors'):
            for obj_id, interaction_type in game_objects[obj_type].items():
                if prefab_name := self._get_prefab_name(obj_id):
                    results[obj_type].add((prefab_name, interaction_type))
        return results

    def get_ui_objects(self):
        '''
        Get all ui objects from the scene and prefabs
        '''
        default_ui_interaction_type = "activate"  # set the default interaction types for all uis to "activate"
        # scene ui objects
        scene_uis = set()
        delegates = self.scene_doc.filter(
            class_names=("MonoBehaviour",), attributes=("m_Delegates",))
        for delegate in delegates:
            object = self.get_entry_by_anchor(
                delegate.m_GameObject.get("fileID"))
            scene_uis.add((object.m_Name, default_ui_interaction_type))

        def has_delegates_in_prefab(prefab_path, processed):
            '''
            Recursive search method to get nested prefab ui objects
            '''
            if prefab_path in processed:
                return False
            processed.add(prefab_path)
            try:
                prefab_doc = UnityDocument.load_yaml(prefab_path)
                if prefab_doc.filter(class_names=("MonoBehaviour",), attributes=("m_Delegates",)):
                    return True
                for instance in prefab_doc.filter(class_names=("PrefabInstance",), attributes=("m_SourcePrefab",)):
                    if source_guid := instance.m_SourcePrefab.get("guid"):
                        for meta_file in self.get_assets("*.prefab.meta"):
                            if self.get_file_guid(meta_file) == source_guid:
                                nested_path = meta_file.parent / meta_file.stem
                                if has_delegates_in_prefab(nested_path, processed):
                                    return True
            except Exception as e:
                print(f"Error processing prefab {prefab_path}: {e}")
            return False
        # prefab ui objects
        prefab_uis = set()
        processed_prefabs = set()
        for name, path in self.get_prefabs_source_from_scene().items():
            if has_delegates_in_prefab(path, processed_prefabs):
                prefab_uis.add((name, default_ui_interaction_type))
        return scene_uis.union(prefab_uis)

    @log_execution_time
    def get_interactors_interactables(self):
        '''
        Categorise scene interactives and interactive prefabs into interactables and interactors
        Returns: Dictionary with two lists - 'interactables' and 'interactors'
        '''
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
        # print(interactive_prefabs['interactables'],
        #       len(interactive_prefabs['interactables']))
        # print(interactive_prefabs['interactors'],
        #       len(interactive_prefabs['interactors']))
        # self.get_interactors_interactables()
        self.build_graph()


if __name__ == '__main__':
    # need to set the path of the scene, and also the path of where the prefabs are stored
    root = Path("/Users/ruizhengu/Projects/InteractoBot/envs/XRIExample")
    # assets = root / "Assets"
    # scenes = assets / "Scenes"
    sut = root / "Assets/Scenes/SampleScene.unity"

    graph = InteractionGraph(root, sut)
    # graph.test()
    # print(graph.get_asset_name_by_guid("3549fdaf258e11846b85a316c16c699c"))
    for prefab in graph.get_prefabs():
        print(prefab)
