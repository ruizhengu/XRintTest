import re
from pathlib import Path

import yaml
from unityparser import UnityDocument
import yaml
import networkx as nx
import matplotlib.pyplot as plt
import time
import functools
import itertools as it
from interactable import Interactable
from interactor import Interactor
from prefab import Prefab, PrefabType
from interaction import Interaction, InteractionEvent, InteractionRole
from loguru import logger
from enum import StrEnum
import utils
import json


def log_execution_time(func):
    """
    Decorator to log the execution time of a function
    """

    @functools.wraps(func)
    def wrapper(*args, **kwargs):
        start_time = time.time()
        result = func(*args, **kwargs)
        end_time = time.time()
        logger.info(f"Function: {func.__name__} Execution time: {end_time - start_time} seconds")
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

class InteractableType(StrEnum):
    THREE_D = "3d"
    TWO_D = "2d"

class InteractionGraph:
    def __init__(self, root, sut):
        self.root = root
        self.asset_path = [
            self.root / "Assets",
            self.root / "Library"
        ]
        self.script_path = self.root / "Assets" / "Scripts"
        self.sut = sut
        self.scene_doc = UnityDocument.load_yaml(self.sut)
        self.interaction_events_3d = self.get_interaction_types_3d()
        self.interaction_events_ui = self.get_interaction_types_ui()
        self.interactables = set()
        self.interactors = set()

    def get_predefined_interactions(self):
        yml = Path('./interaction.yml')
        with open(yml) as f:
            data = yaml.load(f, Loader=yaml.FullLoader)
            # print(data)
        return data

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
        Get the guids of all prefabs in the asset path by
        recording the prefab file names with the guids as keys
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
        prefabs = set()
        asset_prefab_guids = self._get_asset_prefab_guids()
        scene_prefab_instances = doc.filter(
            class_names=("PrefabInstance",), attributes=("m_SourcePrefab",))
        for instance in scene_prefab_instances:
            # print(self._get_prefab_instance_name(instance.anchor))
            if hasattr(instance, "m_IsActive") and instance.m_IsActive != 1:
                continue
            prefab_guid = instance.m_SourcePrefab.get("guid")
            if prefab_guid in asset_prefab_guids:
                # prefab_name = asset_prefab_guids[prefab_guid].stem.replace(".prefab", "")
                # TODO: if use there approach, cannot identify Blaster
                if self._get_prefab_instance_name(instance.anchor):
                    prefab_name = self._get_prefab_instance_name(instance.anchor)
                else:
                    prefab_name = asset_prefab_guids[prefab_guid].stem.replace(".prefab", "")
                prefab_path = asset_prefab_guids[prefab_guid].parent / \
                            asset_prefab_guids[prefab_guid].stem
                interaction_layer = self.get_interaction_layer(
                    instance=instance, )
                # TODO: could set the prefab type according to the input doc path
                prefab = Prefab(name=prefab_name,
                                guid=prefab_guid,
                                file=prefab_path,
                                type=PrefabType.SCENE,
                                interaction_layer=interaction_layer)
                prefabs.add(prefab)
                    # if prefab_child := self._get_nested_prefab(prefab):
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
    
    def _get_prefab_instance_name(self, obj_id):
        """Helper to get prefab name from object ID"""
        if entry := self.get_entry_by_anchor(obj_id):
            # if prefab_id := entry.m_PrefabInstance.get("fileID"):
            #     if prefab_entry := self.get_entry_by_anchor(prefab_id):
            for mod in entry.m_Modification["m_Modifications"]:
                if mod.get("propertyPath") == "m_Name":
                    return mod.get("value")
        return None

    def get_scene_prefabs(self):
        """
        Get the prefabs in the scene under test
        """
        return self._get_prefab_objects(self.scene_doc)

    def get_interaction_layer(self, instance=None, prefab_source=None, obj_id=None):
        """
        Get the interaction layer of the prefab instance
        """
        # If there are modifications related to the interaction layer is done within the scene
        if instance:
            return utils.get_interaction_layer_modification(instance.m_Modification["m_Modifications"])
        elif prefab_source:
            # TODO: If there are no modifications related to the interaction layer, check the prefab source
            pass
        elif obj_id:
            if entry := self.get_entry_by_anchor(obj_id):
                if prefab_id := entry.m_PrefabInstance.get("fileID"):
                    if prefab_entry := self.get_entry_by_anchor(prefab_id):
                        return utils.get_interaction_layer_modification(
                            prefab_entry.m_Modification["m_Modifications"])
        return -1  # Assume the default interaction layer is -1

    def get_interaction_types(self, is_ui=False):
        """
        Get interaction types from scripts based on predefined patterns.
        Args:
            is_ui: Whether to get UI interactions (True) or 3D interactions (False)
        Returns:
            Set of Interaction objects
        """
        predefined_interactions = self.get_predefined_interactions()
        interactions = set()
        processed_guids = set()  # record processed guids to avoid duplicated interactions
        for asset in self.get_assets("*.cs.*"):
            file_name = asset.stem.replace(".cs", "")  # Get the file name without the suffix
            cs_file = asset.parent / asset.stem
            # Skip deprecated and affordance files
            if "deprecated" in file_name or "Affordance" in file_name:
                continue
            guid = self.get_file_guid(asset)
            if guid in processed_guids:
                continue
            if is_ui:
                interaction_event = utils.get_interaction_ui_event(predefined_interactions, file_name)
                if interaction_event:
                    interaction = Interaction(name=file_name,
                                           file=cs_file,
                                           guid=guid,
                                           event=interaction_event,
                                           role=InteractionRole.INTERACTABLE)
                    interactions.add(interaction)
            else:
                interaction_event, interaction_role = utils.get_interaction_event_role(predefined_interactions, file_name)
                if interaction_event:
                    interaction = Interaction(name=file_name,
                                           file=cs_file,
                                           guid=guid,
                                           event=interaction_event,
                                           role=interaction_role)
                    interactions.add(interaction)

            processed_guids.add(guid)
        return interactions

    def get_interaction_types_ui(self):
        return self.get_interaction_types(is_ui=True)

    def get_interaction_types_3d(self):
        return self.get_interaction_types(is_ui=False)

    def _process_prefab_interactions(self, prefab):
        """
        Process a prefab to identify and categorise its interaction components.
        Args:
            prefab: The prefab object to analyse
        Returns:
            set: Collection of Interactable and Interactor objects found in the prefab
        """
        prefab_doc = UnityDocument.load_yaml(prefab.file)
        results = set()
        # Get all script GUIDs from the prefab
        script_guids = {script.m_Script.get("guid") for script in
                       prefab_doc.filter(class_names=("MonoBehaviour",), attributes=("m_Script",))}
        # Find the first matching interaction type
        for interaction in self.interaction_events_3d:
            if interaction.guid not in script_guids:
                continue
            # Base component properties
            component_props = {
                "name": prefab.name,
                "script": interaction.file,
                # "type": InteractableType.THREE_D,
                "event": interaction.event,
                "layer": prefab.interaction_layer
            }
            if interaction.role == InteractionRole.INTERACTOR:
                results.add(Interactor(**component_props))
            elif interaction.role == InteractionRole.INTERACTABLE:
                component_props["type"] = InteractableType.THREE_D
                results.add(Interactable(**component_props))                
                # Add activate interaction if precondition exists
                if utils.has_precondition(prefab_doc):
                    component_props["event"] = InteractionEvent.ACTIVATE
                    results.add(Interactable(**component_props))
            break  # Only process first matching interaction
        return results

    def _get_nested_prefab(self, prefab):
        """
        Helper method to get paths of nested prefabs
        Returns: List of Path objects for nested prefabs
        """
        prefab_doc = UnityDocument.load_yaml(prefab.file)
        return self._get_prefab_objects(prefab_doc)

    @cache_result
    def _has_valid_interactions(self, prefab, processed=None):
        """
        Check if a prefab or any of its children (at any depth) have valid interactions
        Args:
            prefab: The prefab to check
            processed: Set of processed prefabs to avoid cycles
        Returns:
            bool: True if prefab or any of its children have valid interactions
        """
        if processed is None:
            processed = set()
        if prefab in processed:
            return False
        processed.add(prefab)
        # Check current prefab for interactions
        current_results = self._process_prefab_interactions(prefab)
        if current_results:  # If current prefab has interactions
            return True
        # Recursively check children
        for child_prefab in self._get_nested_prefab(prefab):
            if self._has_valid_interactions(child_prefab, processed):
                return True
        return False

    def _build_prefab_hierarchy(self, prefab, processed=None):
        """
        Build the prefab hierarchy by adding children to each prefab
        Args:
            prefab: The root prefab to process
            processed: Set of processed prefabs to avoid cycles
        Returns:
            None
        """
        if processed is None:
            processed = set()
        if prefab in processed:
            return
        processed.add(prefab)
        # Add all children to the prefab hierarchy
        for child_prefab in self._get_nested_prefab(prefab):
            prefab.add_child(child_prefab)
            self._build_prefab_hierarchy(child_prefab, processed)

    def get_interactive_prefabs(self):
        """
        Get prefab instances from the scene that have valid interactions either directly
        or through their children (at any depth).
        Returns: Dictionary with sets of interactables and interactors
        """
        def process_prefab_hierarchy(prefab, processed):
            """
            Process a prefab and its entire hierarchy to find interactions
            Args:
                prefab: The root prefab to process
                processed: Set of processed prefabs to avoid cycles
            Returns:
                set: Set of Interactable and Interactor objects found in the hierarchy
            """
            if prefab in processed:
                return set()
            processed.add(prefab)
            results = set()
            # Process current prefab's interactions
            current_results = self._process_prefab_interactions(prefab)
            for result in current_results:
                results.add(result)
            # Process nested prefabs
            for child_prefab in prefab.children:
                child_results = process_prefab_hierarchy(child_prefab, processed)
                results.update(child_results)
            return results
        processed_prefabs = set()
        # First build the prefab hierarchy for all prefabs
        for prefab_source in self.get_scene_prefabs():
            self._build_prefab_hierarchy(prefab_source)
        # Then process only prefabs that have valid interactions
        for prefab_source in self.get_scene_prefabs():
            if self._has_valid_interactions(prefab_source):
                results_tmp = process_prefab_hierarchy(prefab_source, processed_prefabs)
                for tmp_result in results_tmp:
                    if isinstance(tmp_result, Interactable):
                        self.interactables.add(tmp_result)
                    elif isinstance(tmp_result, Interactor):
                        self.interactors.add(tmp_result)

    def get_scene_interactions(self):
        """
        Get the interactables and interactors in the scene under test
        Returns: Two sets of interactable and interactor objects
        """
        scene_scripts = self.scene_doc.filter(class_names=(
            "MonoBehaviour",), attributes=("m_Script",))
        for script in scene_scripts:
            for interaction in self.interaction_events_3d:
                if interaction.guid != script.m_Script.get("guid"):
                    continue
                # Get the file id of the game object linked to the interactive script
                obj_id = script.m_GameObject.get("fileID")
                if interaction.role == InteractionRole.INTERACTABLE:
                    interactable = Interactable(
                        name=self._get_prefab_name(obj_id),
                        script=interaction.file,
                        type=InteractableType.THREE_D,
                        event=interaction.event,
                        layer=self.get_interaction_layer(obj_id=obj_id), )
                    self.interactables.add(interactable)
                elif interaction.role == InteractionRole.INTERACTOR:
                    interactor = Interactor(
                        name=self._get_prefab_name(obj_id),
                        script=interaction.file,
                        event=interaction.event,
                        layer=self.get_interaction_layer(obj_id=obj_id), )
                    self.interactors.add(interactor)

    def get_linked_object_name(self, anchor):
        entry = self.get_entry_by_anchor(anchor)
        if hasattr(entry, "m_Name"):
            return entry.m_Name
        return None

    def get_ui_objects(self):
        """
        Get all UI objects from the scene and prefabs by checking for interaction events
        """
        default_ui_interaction_layer = -2  # UI objects do not have interaction layers, set value to -2
        # Scene UI objects
        scene_scripts = self.scene_doc.filter(
            class_names=("MonoBehaviour",), attributes=("m_Script",))
        for script in scene_scripts:
            linked_object = script.m_GameObject["fileID"]
            name = self.get_linked_object_name(linked_object)
            if not name:
                name = self._get_prefab_name(obj_id)
            obj_id = script.m_GameObject.get("fileID")
            for interaction in self.interaction_events_ui:
                if interaction.guid != script.m_Script.get("guid"):
                    continue
                # Get the file id of the game object linked to the interactive script
                if interaction.role == InteractionRole.INTERACTABLE:
                    ui = Interactable(
                        name=name,
                        script=interaction.file,
                        type=InteractableType.TWO_D,
                        event=interaction.event,
                        layer=default_ui_interaction_layer
                    )
                    self.interactables.add(ui)
        # Prefab UI objects
        processed_prefabs = set()
        for prefab_source in self.get_scene_prefabs():
            if prefab_source in processed_prefabs:
                continue
            processed_prefabs.add(prefab_source)
            prefab_doc = UnityDocument.load_yaml(prefab_source.file)
            script_guids = {script.m_Script.get("guid") for script in
                          prefab_doc.filter(class_names=("MonoBehaviour",), attributes=("m_Script",))}
            for interaction in self.interaction_events_ui:
                if interaction.guid not in script_guids:
                    continue
                if interaction.role == InteractionRole.INTERACTABLE:
                    ui = Interactable(
                        name=prefab_source.name,
                        script=interaction.file,
                        type=InteractableType.TWO_D,
                        event=interaction.event,
                        layer=default_ui_interaction_layer
                    )
                    self.interactables.add(ui)

    def build_graph(self):
        G = nx.MultiDiGraph()
        connectionstyles = [f"arc3,rad={r}" for r in it.accumulate([0.15] * 4)]
        plt.figure(figsize=(12, 10))
        colors = {
            InteractionEvent.SELECT: 'lightcoral',
            InteractionEvent.ACTIVATE: 'lightsteelblue',
            InteractionEvent.SOCKET: 'khaki',
        }
        # Add nodes and edges
        interactor_user = None
        interactor_socket = set()
        for interactor in self.interactors:
            if InteractionEvent.SOCKET in interactor.event:
                G.add_node(interactor.name)
                interactor_socket.add(interactor)
        for interactor in self.interactors:
            if interactor.name == "XR Origin (XR Rig)":
                interactor_user = interactor
                G.add_node(interactor.name)
                break
        edges_by_type = {}
        for interactable in self.interactables:
            G.add_node(interactable.name)
            G.add_edge(interactor_user.name, interactable.name, interactable.event)
            edges_by_type.setdefault(interactable.event, []).append((interactor_user.name, interactable.name))
            for socket in interactor_socket:
                if socket.interaction_layer == interactable.interaction_layer:
                    G.add_edge(interactor_user.name, interactable.name)
                    edges_by_type.setdefault(InteractionEvent.SOCKET, []).append((socket.name, interactable.name))
        pos = nx.spring_layout(G)
        nx.draw_networkx_nodes(G, pos, node_size=60)
        nx.draw_networkx_labels(G, pos, font_size=10)
        # Draw edges for each interaction type
        for i, (edge_type, edges) in enumerate(edges_by_type.items()):
            nx.draw_networkx_edges(
                G, pos,
                edgelist=edges,
                edge_color=colors.get(edge_type, 'black'),
                connectionstyle=connectionstyles[i % len(connectionstyles)]
            )
            edge_labels = {(u, v): edge_type for u, v in edges}
            nx.draw_networkx_edge_labels(G, pos, edge_labels=edge_labels, font_size=10)
        plt.show()
        # return self._sort_graph_results(G, edges_by_type)

    def sort_graph_results(self):
        # Get socket interactors
        socket_interactors = {i for i in self.interactors if InteractionEvent.SOCKET in i.event}
        results = []
        for interactable in self.interactables:
            # Add base interaction with XR Rig
            results.append({
                "interactor": "XR Origin (XR Rig)",
                "condition": [],
                "interactable": interactable.name,
                "type": interactable.type,
                "event": interactable.event
            })
            # Add socket interactions if layers match
            for socket in socket_interactors:
                if socket.interaction_layer == interactable.interaction_layer:
                    results.append({
                        "interactor": socket.name,
                        "condition": [], 
                        "interactable": interactable.name,
                        "type": InteractionEvent.SOCKET,
                        "event": interactable.event
                    })
        # Check for duplicate interactables and set condition for activate events
        interactable_names = {}
        for result in results:
            name = result["interactable"]
            if name in interactable_names:
                # If this is an activate event and we've seen this name before
                if result["event"] == "activate":
                    result["condition"] = ["select"]
            else:
                interactable_names[name] = True
        # Convert to JSON and save
        output_path = self.script_path / "interaction_results.json"
        with open(output_path, 'w') as f:
            json.dump(results, f, default=str, indent=4)

    def test(self):
        # self.get_interactors_interactables()
        self.get_interactive_prefabs()
        self.get_scene_interactions()
        self.get_ui_objects()
        # print(len(self.interactables))
        # print(len(self.interactors))
        # self.build_graph()
        self.sort_graph_results()


if __name__ == '__main__':
    # need to set the path of the scene, and also the path of where the prefabs are stored
    project_root = Path("/Users/ruizhengu/Projects/InteractoBot/envs/VR Template")
    scene_under_test = project_root / "Assets/Scenes/SampleScene.unity"

    graph = InteractionGraph(project_root, scene_under_test)
    graph.test()
    # print(graph.get_asset_name_by_guid("4e29b1a8efbd4b44bb3f3716e73f07ff"))
