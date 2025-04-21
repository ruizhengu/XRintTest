class Interactable:
    def __init__(self, name, script, type, event, layer):
        self.name = name
        self.script = script
        self.type = type
        self.event = event
        self.layer = layer

    def __str__(self):
        return f"{self.name} (type: {self.type} event: {self.event})"
