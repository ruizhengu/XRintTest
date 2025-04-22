import re
from pathlib import Path
import yaml
from unityparser import UnityDocument
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
    """Decorator to log the execution time of a function"""
    @functools.wraps(func)
    def wrapper(*args, **kwargs):
        start_time = time.time()
        result = func(*args, **kwargs)
        end_time = time.time()
        logger.info(f"Function: {func.__name__} Execution time: {end_time - start_time} seconds")
        return result
    return wrapper


class InteractableType(StrEnum):
    THREE_D = "3d"
    TWO_D = "2d"


class InteractionGraph:
    def __init__(self, root, sut):
        self.root = root
        self.asset_path = [self.root / "Assets", self.root / "Library"]
        self.script_path = self.root / "Assets" / "Scripts"
        self.sut = sut
        self.scene_doc = UnityDocument.load_yaml(self.sut)
        self.scene_scripts = self.scene_doc.filter(class_names=("MonoBehaviour",), attributes=("m_Script",))
        self.interaction_events_3d = self.get_interaction_types_3d()
        self.interaction_events_ui = self.get_interaction_types_ui()
        self.interactables = set()
        self.interactors = set()
        self.prefab_hierarchy = {}  # Track prefab hierarchies
        self.prefab_instances = {}  # Map prefab instances to their top-level names
        self.object_to_prefab = {}  # Map object IDs to their containing prefab
        self.asset_prefab_guids = self._get_asset_prefab_guids()

    def get_predefined_interactions(self):
        yml = Path('./interaction.yml')
        with open(yml) as f:
            return yaml.load(f, Loader=yaml.FullLoader)

    def get_assets(self, suffix="*.meta"):
        """Get all assets based on the asset paths"""
        assets = set()
        for path in self.asset_path:
            for asset in path.rglob(suffix):
                assets.add(asset)
        return assets

    def get_asset_prefabs(self):
        """Get all the prefabs in the asset path"""
        return self.get_assets("*.prefab.meta")

    def get_asset_name_by_guid(self, guid):
        """Get the asset name by the guid"""
        for asset in self.get_assets():
            if found_guid := utils.get_file_guid(asset):
                if found_guid == guid:
                    return asset.stem
        return None

    def _get_asset_prefab_guids(self):
        """Get the guids of all prefabs in the asset path"""
        asset_prefab_guids = {}
        for meta_file in self.get_asset_prefabs():
            if guid := utils.get_file_guid(meta_file):
                asset_prefab_guids[guid] = meta_file
        return asset_prefab_guids

    def get_top_level_prefab_name(self, prefab_or_obj_id):
        """Get the top-level name of a prefab or object"""
        if isinstance(prefab_or_obj_id, Prefab):
            prefab_id = prefab_or_obj_id.guid
            if prefab_id in self.prefab_hierarchy:
                parent_id = self.prefab_hierarchy[prefab_id]
                while parent_id in self.prefab_hierarchy:
                    parent_id = self.prefab_hierarchy[parent_id]
                for prefab in self.get_scene_prefabs():
                    if prefab.guid == parent_id:
                        return prefab.name
            return prefab_or_obj_id.name
        else:
            obj_id = prefab_or_obj_id
            # First check if this object is part of a prefab instance
            if name := utils.get_prefab_instance_name(self.scene_doc, obj_id):
                return name
            # If not a prefab instance, check if it's part of a prefab
            if obj_id in self.object_to_prefab:
                return self.object_to_prefab[obj_id]
            # If all else fails, return the object's own name
            return utils.get_object_name(self.scene_doc, obj_id)

    def _build_prefab_hierarchy_map(self):
        """Build the prefab hierarchy map for the entire scene"""
        self.prefab_hierarchy = {}
        for entry in self.scene_doc.filter(class_names=("PrefabInstance",)):
            prefab_guid = entry.m_SourcePrefab.get("guid")
            if not prefab_guid:
                continue
            prefab_name = utils.get_prefab_instance_name(self.scene_doc, entry.anchor)
            if not prefab_name:
                prefab_name = self.get_asset_name_by_guid(prefab_guid)
            self.prefab_instances[entry.anchor] = prefab_name
            self._process_nested_prefabs(prefab_guid, prefab_name, entry.anchor)

    def _process_nested_prefabs(self, prefab_guid, parent_name, parent_anchor):
        """Process nested prefabs to build the hierarchy map"""
        prefab_meta = None
        for meta_file in self.get_asset_prefabs():
            if utils.get_file_guid(meta_file) == prefab_guid:
                prefab_meta = meta_file
                break
        if not prefab_meta:
            return
        prefab_file = prefab_meta.parent / meta_file.stem
        try:
            prefab_doc = UnityDocument.load_yaml(prefab_file)
            for entry in prefab_doc.entries:
                if hasattr(entry, "m_GameObject"):
                    obj_id = entry.m_GameObject.get("fileID")
                    if obj_id:
                        self.object_to_prefab[obj_id] = parent_name
            for nested_entry in prefab_doc.filter(class_names=("PrefabInstance",)):
                nested_guid = nested_entry.m_SourcePrefab.get("guid")
                if nested_guid:
                    self.prefab_hierarchy[nested_guid] = prefab_guid
                    nested_name = f"{parent_name}/{utils.get_prefab_instance_name(prefab_doc, nested_entry.anchor)}"
                    self._process_nested_prefabs(nested_guid, parent_name, nested_entry.anchor)
        except Exception as e:
            logger.error(f"Error processing prefab {prefab_file}: {e}")

    def get_scene_prefabs(self):
        """Get the prefabs in the scene under test"""
        prefabs = set()
        for instance in self.scene_doc.filter(class_names=("PrefabInstance",), attributes=("m_SourcePrefab",)):
            if hasattr(instance, "m_IsActive") and instance.m_IsActive != 1:
                continue
            prefab_guid = instance.m_SourcePrefab.get("guid")
            if prefab_guid not in self.asset_prefab_guids:
                continue
            prefab_name = utils.get_prefab_instance_name(self.scene_doc, instance.anchor)
            if not prefab_name:
                prefab_name = self.asset_prefab_guids[prefab_guid].stem.replace(".prefab", "")
            prefab_path = self.asset_prefab_guids[prefab_guid].parent / self.asset_prefab_guids[prefab_guid].stem
            interaction_layer = self.get_interaction_layer(instance=instance)
            prefab = Prefab(name=prefab_name,
                            guid=prefab_guid,
                            file=prefab_path,
                            type=PrefabType.SCENE,
                            interaction_layer=interaction_layer)
            prefabs.add(prefab)
        return prefabs

    def get_interaction_layer(self, instance=None, prefab_source=None, obj_id=None):
        """Get the interaction layer of the prefab instance"""
        if instance:
            return utils.get_interaction_layer_modification(instance.m_Modification["m_Modifications"])
        elif obj_id:
            if entry := utils.get_entry_by_anchor(self.scene_doc, obj_id):
                if prefab_id := entry.m_PrefabInstance.get("fileID"):
                    if prefab_entry := utils.get_entry_by_anchor(self.scene_doc, prefab_id):
                        return utils.get_interaction_layer_modification(prefab_entry.m_Modification["m_Modifications"])
        return -1

    def get_interaction_types(self, is_ui=False):
        """Get interaction types from scripts based on predefined patterns"""
        predefined_interactions = self.get_predefined_interactions()
        interactions = set()
        processed_guids = set()
        for asset in self.get_assets("*.cs.*"):
            file_name = asset.stem.replace(".cs", "")
            if "deprecated" in file_name or "Affordance" in file_name:
                continue
            guid = utils.get_file_guid(asset)
            if guid in processed_guids:
                continue
            if is_ui:
                interaction_event = utils.get_interaction_ui_event(predefined_interactions, file_name)
                if interaction_event:
                    interaction = Interaction(name=file_name,
                                              file=asset.parent / asset.stem,
                                              guid=guid,
                                              event=interaction_event,
                                              role=InteractionRole.INTERACTABLE)
                    interactions.add(interaction)
            else:
                interaction_event, interaction_role = utils.get_interaction_event_role(
                    predefined_interactions, file_name)
                if interaction_event:
                    interaction = Interaction(name=file_name,
                                              file=asset.parent / asset.stem,
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
        """Process a prefab to identify and categorise its interaction components"""
        prefab_doc = UnityDocument.load_yaml(prefab.file)
        results = set()
        script_guids = {script.m_Script.get("guid") for script in prefab_doc.filter(
            class_names=("MonoBehaviour",), attributes=("m_Script",))}
        for interaction in self.interaction_events_3d:
            if interaction.guid not in script_guids:
                continue
            top_level_name = self.get_top_level_prefab_name(prefab)
            component_props = {
                "name": top_level_name,
                "script": interaction.file,
                "event": interaction.event,
                "layer": prefab.interaction_layer
            }
            if interaction.role == InteractionRole.INTERACTOR:
                results.add(Interactor(**component_props))
            else:
                component_props["type"] = InteractableType.THREE_D
                results.add(Interactable(**component_props))
                if utils.has_precondition(prefab_doc):
                    component_props["event"] = InteractionEvent.ACTIVATE
                    results.add(Interactable(**component_props))
            break
        return results

    def _has_valid_interactions(self, prefab, processed=None):
        """Check if a prefab or any of its children have valid interactions"""
        if processed is None:
            processed = set()
        if prefab in processed:
            return False
        processed.add(prefab)
        if self._process_prefab_interactions(prefab):
            return True
        for child_prefab in prefab.children:
            if self._has_valid_interactions(child_prefab, processed):
                return True
        return False

    def _build_prefab_hierarchy(self, prefab, processed=None):
        """Build the prefab hierarchy by adding children to each prefab"""
        if processed is None:
            processed = set()
        if prefab in processed:
            return
        processed.add(prefab)
        for child_prefab in self._get_nested_prefab(prefab):
            prefab.add_child(child_prefab)
            self.prefab_hierarchy[child_prefab.guid] = prefab.guid
            self._build_prefab_hierarchy(child_prefab, processed)

    def _get_nested_prefab(self, prefab):
        """Get paths of nested prefabs"""
        prefab_doc = UnityDocument.load_yaml(prefab.file)
        return self._get_prefab_objects(prefab_doc)

    def _get_prefab_objects(self, doc):
        """Get all prefabs within a Unity doc file"""
        prefabs = set()
        for instance in doc.filter(class_names=("PrefabInstance",), attributes=("m_SourcePrefab",)):
            if hasattr(instance, "m_IsActive") and instance.m_IsActive != 1:
                continue
            prefab_guid = instance.m_SourcePrefab.get("guid")
            if prefab_guid not in self.asset_prefab_guids:
                continue
            prefab_name = utils.get_prefab_instance_name(doc, instance.anchor)
            if not prefab_name:
                prefab_name = self.asset_prefab_guids[prefab_guid].stem.replace(".prefab", "")
            prefab_path = self.asset_prefab_guids[prefab_guid].parent / self.asset_prefab_guids[prefab_guid].stem
            interaction_layer = self.get_interaction_layer(instance=instance)
            prefab = Prefab(name=prefab_name,
                            guid=prefab_guid,
                            file=prefab_path,
                            type=PrefabType.SCENE,
                            interaction_layer=interaction_layer)
            prefabs.add(prefab)
        return prefabs

    def get_interactive_prefabs(self):
        """Get prefab instances from the scene that have valid interactions"""
        def process_prefab_hierarchy(prefab, processed):
            if prefab in processed:
                return set()
            processed.add(prefab)
            results = set()
            current_results = self._process_prefab_interactions(prefab)
            results.update(current_results)
            for child_prefab in prefab.children:
                child_results = process_prefab_hierarchy(child_prefab, processed)
                results.update(child_results)
            return results

        processed_prefabs = set()
        self._build_prefab_hierarchy_map()
        for prefab_source in self.get_scene_prefabs():
            self._build_prefab_hierarchy(prefab_source)
        for prefab_source in self.get_scene_prefabs():
            if self._has_valid_interactions(prefab_source):
                results_tmp = process_prefab_hierarchy(prefab_source, processed_prefabs)
                for tmp_result in results_tmp:
                    if isinstance(tmp_result, Interactable):
                        self.interactables.add(tmp_result)
                    elif isinstance(tmp_result, Interactor):
                        self.interactors.add(tmp_result)

    def get_scene_interactions(self):
        """Get the interactables and interactors in the scene under test"""
        processed_names = {obj.name for obj in self.interactables} | {obj.name for obj in self.interactors}
        for script in self.scene_scripts:
            for interaction in self.interaction_events_3d:
                if interaction.guid != script.m_Script.get("guid"):
                    continue
                obj_id = script.m_GameObject.get("fileID")
                obj_name = utils.get_prefab_name(self.scene_doc, obj_id)
                if not obj_name or obj_name in processed_names:
                    continue
                if interaction.role == InteractionRole.INTERACTOR:
                    interactor = Interactor(
                        name=obj_name,
                        script=interaction.file,
                        event=interaction.event,
                        layer=self.get_interaction_layer(obj_id=obj_id), )
                    self.interactors.add(interactor)
                    processed_names.add(obj_name)
                elif interaction.role == InteractionRole.INTERACTABLE:
                    interactable = Interactable(
                        name=obj_name,
                        script=interaction.file,
                        type=InteractableType.THREE_D,
                        event=interaction.event,
                        layer=self.get_interaction_layer(obj_id=obj_id), )
                    self.interactables.add(interactable)
                    processed_names.add(obj_name)

    def get_ui_objects(self):
        """Get all UI objects from the scene and prefabs"""
        default_ui_interaction_layer = -2
        processed_names = {obj.name for obj in self.interactables} | {obj.name for obj in self.interactors}

        # Scene UI objects
        for script in self.scene_scripts:
            linked_object = script.m_GameObject["fileID"]
            name = utils.get_object_name(self.scene_doc, linked_object)
            if not name:
                name = self.get_top_level_prefab_name(linked_object)
            if not name or name in processed_names:
                continue
            for interaction in self.interaction_events_ui:
                if interaction.guid != script.m_Script.get("guid"):
                    continue
                if interaction.role == InteractionRole.INTERACTABLE:
                    ui = Interactable(
                        name=name,
                        script=interaction.file,
                        type=InteractableType.TWO_D,
                        event=interaction.event,
                        layer=default_ui_interaction_layer
                    )
                    self.interactables.add(ui)
                    processed_names.add(name)

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
                    name = self.get_top_level_prefab_name(prefab_source)
                    if not name or name in processed_names:
                        continue
                    ui = Interactable(
                        name=name,
                        script=interaction.file,
                        type=InteractableType.TWO_D,
                        event=interaction.event,
                        layer=default_ui_interaction_layer
                    )
                    self.interactables.add(ui)
                    processed_names.add(name)

    def build_graph(self):
        """Build the interaction graph"""
        G = nx.MultiDiGraph()
        connectionstyles = [f"arc3,rad={r}" for r in it.accumulate([0.15] * 4)]
        plt.figure(figsize=(12, 10))
        colors = {
            InteractionEvent.SELECT: 'lightcoral',
            InteractionEvent.ACTIVATE: 'lightsteelblue',
            InteractionEvent.SOCKET: 'khaki',
        }
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

    def sort_graph_results(self):
        """Sort and save the graph results"""
        socket_interactors = {i for i in self.interactors if InteractionEvent.SOCKET in i.event}
        results = []
        for interactable in self.interactables:
            results.append({
                "interactor": "XR Origin (XR Rig)",
                "condition": [],
                "interactable": interactable.name,
                "type": interactable.type,
                "event_type": interactable.event
            })
            for socket in socket_interactors:
                if socket.interaction_layer == interactable.interaction_layer:
                    results.append({
                        "interactor": socket.name,
                        "condition": [],
                        "interactable": interactable.name,
                        "type": InteractionEvent.SOCKET,
                        "event_type": interactable.event
                    })
        interactable_names = {}
        for result in results:
            name = result["interactable"]
            if name in interactable_names:
                if result["event_type"] == "activate":
                    result["condition"] = ["select"]
            else:
                interactable_names[name] = True
        output_path = self.script_path / "interaction_results.json"
        with open(output_path, 'w') as f:
            json.dump(results, f, default=str, indent=4)

    def test(self):
        self.get_interactive_prefabs()
        self.get_scene_interactions()
        self.get_ui_objects()
        print(len(self.interactables))
        print(len(self.interactors))
        # self.build_graph()
        self.sort_graph_results()


if __name__ == '__main__':
    project_root = Path("/Users/ruizhengu/Projects/InteractoBot/envs/VR Template")
    scene_under_test = project_root / "Assets/Scenes/SampleScene.unity"
    graph = InteractionGraph(project_root, scene_under_test)
    graph.test()
