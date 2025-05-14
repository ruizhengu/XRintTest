using UnityEngine;
using System.Collections.Generic;

public class InteractionEvent
{
    public string interactor;
    public List<string> condition;
    public string interactable;
    public string interaction_type;

    public InteractionEvent() { }

    public InteractionEvent(string interactor, List<string> condition, string interactable, string interaction_type)
    {
        this.interactor = interactor;
        this.condition = condition;
        this.interactable = interactable;
        this.interaction_type = interaction_type;
    }
}
