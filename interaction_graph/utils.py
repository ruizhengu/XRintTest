from interaction import Interaction, InteractionType, InteractionRole

def get_interaction_type_role(predefined_interactions, file_name):
  predefined_interactables = predefined_interactions["interactables"]
  predefined_interactors = predefined_interactions["interactors"]
  predefined_activate = predefined_interactables["activate"]
  predefined_select = predefined_interactables["select"]
  predefined_custom = predefined_interactables["custom"]
  predefind_socket = predefined_interactors["socket"]
  predefined_user = predefined_interactors["user"]
  interaction_type = None
  interaction_role = None
  for activate in predefined_activate:
    if file_name == activate:
        interaction_type = InteractionType.ACTIVATE
        interaction_role = InteractionRole.INTERACTABLE
  for select in predefined_select:
    if file_name == select:
        interaction_type = InteractionType.SELECT
        interaction_role = InteractionRole.INTERACTABLE
  for custom in predefined_custom:
    if file_name == custom:
        interaction_type = InteractionType.SELECT
        interaction_role = InteractionRole.INTERACTABLE
  for socket in predefind_socket:
    if file_name == socket:
        interaction_type = InteractionType.SOCKET
        interaction_role = InteractionRole.INTERACTOR
  for user in predefined_user:
    if file_name == user:
        interaction_type = InteractionType.USER
        interaction_role = InteractionRole.INTERACTOR
  # if interaction_type and interaction_role:
  return interaction_type, interaction_role
  # return None
