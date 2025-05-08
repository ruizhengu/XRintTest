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
        # self.interaction_events_ui = self.get_interaction_types_ui()
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

    def has_interaction_scripts(self, prefab_path):
        """Check if a prefab has any interaction scripts"""
        try:
            prefab_doc = UnityDocument.load_yaml(prefab_path)
            script_guids = {script.m_Script.get("guid") for script in prefab_doc.filter(
                class_names=("MonoBehaviour",), attributes=("m_Script",))}
            return any(interaction.guid in script_guids for interaction in self.interaction_events_3d)
        except:
            return False

    def get_prefab_instances(self, target_guid, container_doc):
        """Get all instances of a prefab from a container document"""
        instances = {}

        # First, collect all scene-level modifications
        scene_modifications = {}
        container_guid = container_doc.entries[0].m_SourcePrefab.get("guid")

        # Find all instances in the scene that reference our container prefab
        for scene_instance in self.scene_doc.filter(class_names=("PrefabInstance",)):
            if scene_instance.m_SourcePrefab.get("guid") == container_guid:
                # Get all name modifications from this container's instances
                for mod in scene_instance.m_Modification["m_Modifications"]:
                    if mod.get("propertyPath") == "m_Name":
                        target_id = mod.get("target", {}).get("fileID")
                        if target_id:
                            scene_modifications[target_id] = mod.get("value")
                            print(
                                f"Found scene modification for container {container_guid}: {target_id} -> {mod.get('value')}")

        # Find all instances of our target prefab within the container
        for nested_instance in container_doc.filter(class_names=("PrefabInstance",)):
            nested_guid = nested_instance.m_SourcePrefab.get("guid")
            if nested_guid == target_guid:
                # Get the instance's fileID from its modifications
                for mod in nested_instance.m_Modification["m_Modifications"]:
                    if mod.get("propertyPath") == "m_Name":
                        target_id = mod.get("target", {}).get("fileID")
                        if target_id and target_id in scene_modifications:
                            instances[nested_instance.anchor] = scene_modifications[target_id]
                            print(
                                f"Mapped prefab {target_guid} instance {nested_instance.anchor} to scene name: {scene_modifications[target_id]}")

        print(f"Final instance mapping for prefab {target_guid} in container {container_guid}: {instances}")
        return instances

    def _process_prefab_interactions(self, prefab):
        """
        Process a prefab to identify and categorise its interaction components
        Args:
            prefab: The prefab object to analyse
        """
        prefab_doc = UnityDocument.load_yaml(prefab.file)
        results = set()
        script_guids = {script.m_Script.get("guid") for script in prefab_doc.filter(
            class_names=("MonoBehaviour",), attributes=("m_Script",))}

        # First check if this prefab has any interaction scripts
        has_interaction = False
        interaction_type = None
        for interaction in self.interaction_events_3d:
            if interaction.guid in script_guids:
                has_interaction = True
                interaction_type = interaction
                break

        # If this prefab has interaction scripts, process them
        if has_interaction:
            print(f"\nProcessing prefab with interaction: {prefab.name} (GUID: {prefab.guid})")
            instance_names = {}  # Map of fileID to instance name

            # First check if this prefab is directly referenced in the scene
            direct_instances = {}
            for scene_instance in self.scene_doc.filter(class_names=("PrefabInstance",)):
                if scene_instance.m_SourcePrefab.get("guid") == prefab.guid:
                    for mod in scene_instance.m_Modification["m_Modifications"]:
                        if mod.get("propertyPath") == "m_Name":
                            target_id = mod.get("target", {}).get("fileID")
                            if target_id:
                                instance_name = mod.get("value")
                                print(f"Found direct instance in scene: {instance_name}")
                                direct_instances[target_id] = instance_name

            # Then check for instances in container prefabs
            container_instances = {}
            # First build the chain of parent prefabs
            current_guid = prefab.guid
            parent_chain = []
            while current_guid in self.prefab_hierarchy:
                parent_guid = self.prefab_hierarchy[current_guid]
                parent_chain.append(parent_guid)
                current_guid = parent_guid

            print(f"Parent chain for {prefab.name}: {parent_chain}")

            # Check each parent prefab
            for parent_guid in parent_chain:
                print(f"Checking parent prefab: {parent_guid}")
                container_path = self.asset_prefab_guids[parent_guid].parent / self.asset_prefab_guids[parent_guid].stem
                try:
                    container_doc = UnityDocument.load_yaml(container_path)
                    # Only process if container has no interaction scripts
                    container_scripts = {script.m_Script.get("guid") for script in container_doc.filter(
                        class_names=("MonoBehaviour",), attributes=("m_Script",))}
                    if not any(interaction.guid in container_scripts for interaction in self.interaction_events_3d):
                        print(f"Container {parent_guid} has no interaction scripts, checking for instances")
                        found_instances = self.get_prefab_instances(prefab.guid, container_doc)
                        if found_instances:
                            print(f"Found instances in container: {found_instances}")
                            container_instances.update(found_instances)
                except Exception as e:
                    print(f"Error processing container {parent_guid}: {e}")
                    continue

            # Also check scene instances of containers
            for scene_instance in self.scene_doc.filter(class_names=("PrefabInstance",)):
                instance_guid = scene_instance.m_SourcePrefab.get("guid")
                if instance_guid in parent_chain:
                    print(f"Found container instance in scene: {instance_guid}")
                    container_path = self.asset_prefab_guids[instance_guid].parent / \
                        self.asset_prefab_guids[instance_guid].stem
                    try:
                        container_doc = UnityDocument.load_yaml(container_path)
                        # Only process if container has no interaction scripts
                        container_scripts = {script.m_Script.get("guid") for script in container_doc.filter(
                            class_names=("MonoBehaviour",), attributes=("m_Script",))}
                        if not any(interaction.guid in container_scripts for interaction in self.interaction_events_3d):
                            print(f"Scene container {instance_guid} has no interaction scripts, checking for instances")
                            found_instances = self.get_prefab_instances(prefab.guid, container_doc)
                            if found_instances:
                                print(f"Found instances in scene container: {found_instances}")
                                container_instances.update(found_instances)
                    except Exception as e:
                        print(f"Error processing scene container {instance_guid}: {e}")
                        continue

            # Use the most specific names available
            if container_instances:
                instance_names.update(container_instances)
                print(f"Using container instances: {container_instances}")
            elif direct_instances:
                instance_names.update(direct_instances)
                print(f"Using direct instances: {direct_instances}")
            else:
                instance_names["default"] = prefab.name
                print(f"Using prefab name: {prefab.name}")

            # Create components for each instance
            for file_id, instance_name in instance_names.items():
                component_props = {
                    "name": instance_name,
                    "script": interaction_type.file,
                    "event": interaction_type.event,
                    "layer": prefab.interaction_layer
                }
                if interaction_type.role == InteractionRole.INTERACTOR:
                    results.add(Interactor(**component_props))
                else:
                    component_props["type"] = InteractableType.THREE_D
                    interactable = Interactable(**component_props)
                    print(f"Creating interactable: {interactable.name}")
                    results.add(interactable)
                    # Only add activate event if there's a precondition and we haven't added it yet
                    if utils.has_precondition(prefab_doc):
                        activate_props = component_props.copy()
                        activate_props["event"] = InteractionEvent.ACTIVATE
                        results.add(Interactable(**activate_props))

        # Process nested prefabs regardless of whether this prefab has interactions
        for nested_entry in prefab_doc.filter(class_names=("PrefabInstance",)):
            nested_guid = nested_entry.m_SourcePrefab.get("guid")
            if nested_guid and nested_guid in self.asset_prefab_guids:
                instance_name = utils.get_prefab_instance_name(prefab_doc, nested_entry.anchor)
                if not instance_name:
                    instance_name = self.asset_prefab_guids[nested_guid].stem.replace(".prefab", "")

                nested_prefab = Prefab(
                    name=instance_name,
                    guid=nested_guid,
                    file=self.asset_prefab_guids[nested_guid].parent / self.asset_prefab_guids[nested_guid].stem,
                    type=PrefabType.SCENE,
                    interaction_layer=prefab.interaction_layer
                )
                nested_results = self._process_prefab_interactions(nested_prefab)
                results.update(nested_results)

        return results

    def _has_valid_interactions(self, prefab, processed=None):
        """Check if a prefab or any of its children have valid interactions"""
        if processed is None:
            processed = set()
        if prefab in processed:
            return False
        processed.add(prefab)

        # First check if this prefab has interaction scripts
        prefab_doc = UnityDocument.load_yaml(prefab.file)
        script_guids = {script.m_Script.get("guid") for script in prefab_doc.filter(
            class_names=("MonoBehaviour",), attributes=("m_Script",))}
        for interaction in self.interaction_events_3d:
            if interaction.guid in script_guids:
                return True

        # If not, check nested prefabs
        for nested_entry in prefab_doc.filter(class_names=("PrefabInstance",)):
            nested_guid = nested_entry.m_SourcePrefab.get("guid")
            if nested_guid and nested_guid in self.asset_prefab_guids:
                nested_prefab = Prefab(
                    name=utils.get_prefab_instance_name(prefab_doc, nested_entry.anchor),
                    guid=nested_guid,
                    file=self.asset_prefab_guids[nested_guid].parent / self.asset_prefab_guids[nested_guid].stem,
                    type=PrefabType.SCENE,
                    interaction_layer=prefab.interaction_layer
                )
                if self._has_valid_interactions(nested_prefab, processed):
                    return True
        return False

    def _build_prefab_hierarchy_map(self):
        """Build the prefab hierarchy map for the entire scene"""
        self.prefab_hierarchy = {}
        self.prefab_instances = {}  # Map prefab GUIDs to their instance names

        def process_prefab_hierarchy(doc, parent_guid=None):
            """Process a document to build prefab hierarchy"""
            for instance in doc.filter(class_names=("PrefabInstance",)):
                prefab_guid = instance.m_SourcePrefab.get("guid")
                if not prefab_guid or prefab_guid not in self.asset_prefab_guids:
                    continue

                # Store parent relationship
                if parent_guid:
                    self.prefab_hierarchy[prefab_guid] = parent_guid

                # Process the prefab's own hierarchy
                prefab_path = self.asset_prefab_guids[prefab_guid].parent / self.asset_prefab_guids[prefab_guid].stem
                try:
                    nested_doc = UnityDocument.load_yaml(prefab_path)
                    process_prefab_hierarchy(nested_doc, prefab_guid)
                except:
                    continue

        # First process the scene
        process_prefab_hierarchy(self.scene_doc)

        # Debug output
        print("\nPrefab Hierarchy:")
        for child, parent in self.prefab_hierarchy.items():
            print(f"Child {child} -> Parent {parent}")

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

            # First check if this prefab has interaction scripts
            prefab_doc = UnityDocument.load_yaml(prefab.file)
            script_guids = {script.m_Script.get("guid") for script in prefab_doc.filter(
                class_names=("MonoBehaviour",), attributes=("m_Script",))}
            has_interaction = any(interaction.guid in script_guids for interaction in self.interaction_events_3d)

            if has_interaction:
                # This is an interactive prefab, process it normally
                current_results = self._process_prefab_interactions(prefab)
                results.update(current_results)
            else:
                # This is a container prefab, only process its children
                for nested_entry in prefab_doc.filter(class_names=("PrefabInstance",)):
                    nested_guid = nested_entry.m_SourcePrefab.get("guid")
                    if nested_guid and nested_guid in self.asset_prefab_guids:
                        # Get the instance name from the container prefab
                        instance_name = None
                        for mod in nested_entry.m_Modification["m_Modifications"]:
                            if mod.get("propertyPath") == "m_Name":
                                instance_name = mod.get("value")
                                break

                        if not instance_name:
                            instance_name = self.asset_prefab_guids[nested_guid].stem.replace(".prefab", "")

                        nested_prefab = Prefab(
                            name=instance_name,
                            guid=nested_guid,
                            file=self.asset_prefab_guids[nested_guid].parent /
                            self.asset_prefab_guids[nested_guid].stem,
                            type=PrefabType.SCENE,
                            interaction_layer=prefab.interaction_layer
                        )
                        nested_results = process_prefab_hierarchy(nested_prefab, processed)
                        results.update(nested_results)

            return results

        processed_prefabs = set()
        self._build_prefab_hierarchy_map()

        # First process scene prefabs to build hierarchy
        for prefab_source in self.get_scene_prefabs():
            self._build_prefab_hierarchy(prefab_source)

        # Track what we've already added to avoid duplicates
        seen_items = set()

        # Then process each prefab
        for prefab_source in self.get_scene_prefabs():
            # Check if this is a container prefab
            prefab_doc = UnityDocument.load_yaml(prefab_source.file)
            script_guids = {script.m_Script.get("guid") for script in prefab_doc.filter(
                class_names=("MonoBehaviour",), attributes=("m_Script",))}
            has_interaction = any(interaction.guid in script_guids for interaction in self.interaction_events_3d)

            if not has_interaction:
                # For container prefabs, process their children
                results_tmp = process_prefab_hierarchy(prefab_source, processed_prefabs)
                for tmp_result in results_tmp:
                    # Create a unique key for each item based on name and event
                    key = (tmp_result.name, tmp_result.event)
                    if key not in seen_items:
                        if isinstance(tmp_result, Interactable):
                            self.interactables.add(tmp_result)
                        elif isinstance(tmp_result, Interactor):
                            self.interactors.add(tmp_result)
                        seen_items.add(key)

    def get_scene_interactions(self):
        """
        Get the interactables and interactors in the scene under test
        Returns: Two sets of interactable and interactor objects
        """
        # Track names and events we've already processed to avoid duplicates
        processed = {(obj.name, obj.event) for obj in self.interactables} | {
            (obj.name, obj.event) for obj in self.interactors}

        for script in self.scene_scripts:
            for interaction in self.interaction_events_3d:
                if interaction.guid != script.m_Script.get("guid"):
                    continue
                # Get the file id of the game object linked to the interactive script
                obj_id = script.m_GameObject.get("fileID")

                # First try to get the scene-level instance name
                obj_name = None
                for instance in self.scene_doc.filter(class_names=("PrefabInstance",)):
                    if instance.anchor == obj_id:
                        for mod in instance.m_Modification["m_Modifications"]:
                            if mod.get("propertyPath") == "m_Name":
                                obj_name = mod.get("value")
                                break
                        break

                # If no scene instance name found, try other methods
                if not obj_name:
                    obj_name = utils.get_prefab_instance_name(self.scene_doc, obj_id)
                if not obj_name:
                    obj_name = utils.get_object_name(self.scene_doc, obj_id)

                if not obj_name:
                    continue

                if interaction.role == InteractionRole.INTERACTABLE:
                    if (obj_name, interaction.event) not in processed:
                        interactable = Interactable(
                            name=obj_name,
                            script=interaction.file,
                            type=InteractableType.THREE_D,
                            event=interaction.event,
                            layer=self.get_interaction_layer(obj_id=obj_id), )
                        self.interactables.add(interactable)
                        processed.add((obj_name, interaction.event))
                        if utils.has_precondition(self.scene_doc) and (obj_name, InteractionEvent.ACTIVATE) not in processed:
                            interactable = Interactable(
                                name=obj_name,
                                script=interaction.file,
                                type=InteractableType.THREE_D,
                                event=InteractionEvent.ACTIVATE,
                                layer=self.get_interaction_layer(obj_id=obj_id), )
                            self.interactables.add(interactable)
                            processed.add((obj_name, InteractionEvent.ACTIVATE))
                elif interaction.role == InteractionRole.INTERACTOR:
                    if (obj_name, interaction.event) not in processed:
                        interactor = Interactor(
                            name=obj_name,
                            script=interaction.file,
                            event=interaction.event,
                            layer=self.get_interaction_layer(obj_id=obj_id), )
                        self.interactors.add(interactor)
                        processed.add((obj_name, interaction.event))

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

        # First add all nodes
        for interactor in self.interactors:
            G.add_node(interactor.name)
        for interactable in self.interactables:
            G.add_node(interactable.name)

        # Find the XR Origin interactor
        xr_origin = None
        for interactor in self.interactors:
            if "XR Origin" in interactor.name:
                xr_origin = interactor
                break

        # If no XR Origin found, create a default one
        if not xr_origin:
            xr_origin = Interactor(
                name="XR Origin (XR Rig)",
                script=None,
                event=InteractionEvent.SELECT,
                layer=0
            )
            self.interactors.add(xr_origin)
            G.add_node(xr_origin.name)

        # Add edges
        interactor_socket = {i for i in self.interactors if InteractionEvent.SOCKET in i.event}
        edges_by_type = {}

        for interactable in self.interactables:
            G.add_edge(xr_origin.name, interactable.name, interactable.event)
            edges_by_type.setdefault(interactable.event, []).append((xr_origin.name, interactable.name))
            for socket in interactor_socket:
                if socket.interaction_layer == interactable.interaction_layer:
                    G.add_edge(socket.name, interactable.name)
                    edges_by_type.setdefault(InteractionEvent.SOCKET, []).append((socket.name, interactable.name))

        # Draw the graph
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
        print("=== Interactables ===")
        for interactable in sorted(self.interactables, key=lambda x: x.name):
            print(f"Name: {interactable.name}, Event: {interactable.event}")
        print("\n=== Interactors ===")
        for interactor in sorted(self.interactors, key=lambda x: x.name):
            print(f"Name: {interactor.name}, Event: {interactor.event}")
        print(f"\nTotal Interactables: {len(self.interactables)}")
        print(f"Total Interactors: {len(self.interactors)}")
        # self.build_graph()
        self.sort_graph_results()


if __name__ == '__main__':
    # project_root = Path("/Users/ruizhengu/Projects/InteractoBot/envs/VR Template")
    project_root = Path(
        "/Users/ruizhengu/Projects/XUIBench/XRI Starter Assets/")
    # scene_under_test = project_root / "Assets/Scenes/SampleScene.unity"
    scene_under_test = project_root / "Assets/Samples/XR Interaction Toolkit/3.1.1/Starter Assets/DemoScene.unity"
    graph = InteractionGraph(project_root, scene_under_test)
    graph.test()
