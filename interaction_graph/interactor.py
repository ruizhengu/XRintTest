

class Interactor:
    def __init__(self, name, script, event, layer):
        self.name = name
        self.script = script
        self.event = event
        self.layer = layer

    def __str__(self):
        return f"{self.name} ({self.event})"
