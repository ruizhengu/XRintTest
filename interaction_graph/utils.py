from interaction import Interaction, InteractionEvent, InteractionRole
from typing import Optional, Tuple

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

