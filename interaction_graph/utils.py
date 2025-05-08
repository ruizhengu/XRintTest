import functools
from interaction import Interaction, InteractionEvent, InteractionRole
from typing import Optional, Tuple, Dict, Any
import re
from pathlib import Path


def cache_results(func):
    """Cache function results to avoid redundant computations"""
    cache = {}

    @functools.wraps(func)
    def wrapper(*args, **kwargs):
        key = str(args) + str(kwargs)
        if key not in cache:
            cache[key] = func(*args, **kwargs)
        return cache[key]
    return wrapper


def get_interaction_event_role(predefined_interactions: dict, file_name: str) -> Tuple[Optional[InteractionEvent], Optional[InteractionRole]]:
    """
    Determine the interaction event and role for a given file name based on predefined interactions.

    Args:
        predefined_interactions: Dictionary containing predefined interaction mappings
        file_name: Name of the file to check

    Returns:
        Tuple containing the interaction event and role, or (None, None) if no match found
    """
    # Define the mapping of interaction types to their corresponding events and roles
    interaction_mappings = {
        "interactables": {
            "activate": (InteractionEvent.ACTIVATE, InteractionRole.INTERACTABLE),
            "select": (InteractionEvent.SELECT, InteractionRole.INTERACTABLE),
            "custom": (InteractionEvent.SELECT, InteractionRole.INTERACTABLE)
        },
        "interactors": {
            "socket": (InteractionEvent.SOCKET, InteractionRole.INTERACTOR),
            "user": (InteractionEvent.USER, InteractionRole.INTERACTOR)
        }
    }
    # Check each category and type for a matching file name
    for category, types in interaction_mappings.items():
        for interaction_type, (event, role) in types.items():
            if file_name in predefined_interactions[category][interaction_type]:
                return event, role
    return None, None


def get_interaction_ui_event(predefined_interactions: dict, file_name: str) -> Optional[InteractionEvent]:
    """
    Determine the UI interaction event for a given file name based on predefined UI interactions.
    Args:
        predefined_interactions: Dictionary containing predefined interaction mappings
        file_name: Name of the file to check

    Returns:
        InteractionEvent if a match is found, None otherwise
    """
    # Check if the file name matches any predefined UI activation events
    if file_name in predefined_interactions["uis"]["activate"]:
        return InteractionEvent.ACTIVATE
    return None


def get_interaction_layer_modification(modifications):
    """
    Extract interaction layer value from modifications
    """
    for mod in modifications:
        if mod.get("propertyPath") == "m_InteractionLayers.m_Bits":
            return mod.get("value")


def has_precondition(prefab_doc):
    """
    Check if select interactions also supported activate interactions (m_Activated in MonoBehaviour)
    """
    for entry in prefab_doc.filter(class_names=("MonoBehaviour",), attributes=("m_Activated",)):
        if calls := entry.m_Activated.get("m_PersistentCalls", {}).get("m_Calls"):
            if calls:  # If m_Calls is not empty
                return True
    return False


def get_prefab_name(doc, obj_id):
    """Helper to get prefab name from object ID"""
    if entry := get_entry_by_anchor(doc, obj_id):
        # Check if this is a PrefabInstance entry
        if hasattr(entry, "m_PrefabInstance"):
            if prefab_id := entry.m_PrefabInstance.get("fileID"):
                if prefab_entry := get_entry_by_anchor(doc, prefab_id):
                    # Check if prefab entry has modifications
                    if hasattr(prefab_entry, "m_Modification"):
                        for mod in prefab_entry.m_Modification["m_Modifications"]:
                            if mod.get("propertyPath") == "m_Name":
                                return mod.get("value")
                    # If no modifications, try to get name directly
                    elif hasattr(prefab_entry, "m_Name"):
                        return prefab_entry.m_Name
        # If not a PrefabInstance, try to get the name directly
        elif hasattr(entry, "m_Name"):
            return entry.m_Name
    return None


def get_prefab_instance_name(doc, obj_id):
    """Helper to get prefab name from object ID"""
    if entry := get_entry_by_anchor(doc, obj_id):
        # Check if this is a PrefabInstance entry
        if hasattr(entry, "m_Modification"):
            for mod in entry.m_Modification["m_Modifications"]:
                if mod.get("propertyPath") == "m_Name":
                    return mod.get("value")
        # If not a PrefabInstance, try to get the name directly
        elif hasattr(entry, "m_Name"):
            return entry.m_Name
    return None


@cache_results
def get_entry_by_anchor(doc, anchor):
    """Get entry by anchor from Unity document"""
    for entry in doc.entries:
        if entry.anchor == anchor:
            return entry
    return None


@cache_results
def get_object_name(doc, obj_id: str, check_modifications: bool = True) -> Optional[str]:
    """Get object name from Unity document with optional modification checking"""
    if entry := get_entry_by_anchor(doc, obj_id):
        if check_modifications and hasattr(entry, "m_Modification"):
            name = get_modification_value(entry.m_Modification["m_Modifications"], "m_Name")
            if name:
                return name
        if hasattr(entry, "m_Name"):
            return entry.m_Name
    return None


@cache_results
def get_file_guid(file_path: Path) -> Optional[str]:
    """Get the guid of the file"""
    try:
        content = file_path.read_text(encoding='utf-8')
        if match := re.search(r'guid: (\w+)', content):
            return match.group(1)
    except Exception:
        return None
    return None


@cache_results
def get_object_path(doc, obj_id):
    """Get the full path of an object in the scene hierarchy"""
    if entry := get_entry_by_anchor(doc, obj_id):
        path_parts = []
        if name := get_object_name(doc, obj_id, check_modifications=False):
            path_parts.append(name)

        current_entry = entry
        while hasattr(current_entry, "m_Father"):
            if parent_id := current_entry.m_Father.get("fileID"):
                if parent_entry := get_entry_by_anchor(doc, parent_id):
                    if parent_name := get_object_name(doc, parent_id, check_modifications=False):
                        path_parts.append(parent_name)
                    current_entry = parent_entry
                else:
                    break
            else:
                break

        return "/".join(reversed(path_parts)) if path_parts else None
    return None


@cache_results
def get_modification_value(modifications: list, property_path: str) -> Optional[Any]:
    """Get a modification value for a given property path"""
    for mod in modifications:
        if mod.get("propertyPath") == property_path:
            return mod.get("value")
    return None
