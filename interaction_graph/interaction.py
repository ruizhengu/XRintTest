from enum import Enum, IntEnum


class InteractionType(IntEnum):
    SELECT = 1
    ACTIVATE = 2
    SOCKET = 3
    CUSTOM = 4
    SELECT_TENTATIVE = 5
    ACTIVATE_TENTATIVE = 6


class Interaction:
    def __init__(self, name, file, guid, interaction_type):
        self.name = name
        self.file = file
        self.guid = guid
        self.interaction_type = interaction_type

    def __str__(self):
        return f"{self.name} ({self.interaction_type})"
