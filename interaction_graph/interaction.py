from enum import Enum


class InteractionType(Enum):
    SELECT = 1
    ACTIVATE = 2
    CUSTOM = 3


class Interaction:
    def __init__(self, name, file, guid, interaction_type="select"):
        self.name = name
        self.file = file
        self.guid = guid
        self.interaction_type = interaction_type

    def __str__(self):
        return f"{self.name} ({self.interaction_type})"
