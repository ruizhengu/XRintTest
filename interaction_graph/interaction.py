from enum import Enum, IntEnum, StrEnum


class InteractionType(StrEnum):
    SELECT = "select"
    ACTIVATE = "activate"
    SOCKET = "socket"
    CUSTOM = "custom"


class Interaction:
    def __init__(self, name, file, guid, interaction_type):
        self.name = name
        self.file = file
        self.guid = guid
        self.interaction_type = interaction_type

    def __str__(self):
        return f"{self.name} ({self.interaction_type})"
