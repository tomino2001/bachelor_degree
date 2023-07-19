﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HatchButton : Interactable {

    protected override void ShowInteractMessage () {
        ShipController ship = FindObjectOfType<ShipController> ();
        string action = (ship.HatchOpen) ? "close" : "open";
        string contextualMessage = $"Press F to {action} hatch";
        GameUI.DisplayInteractionInfo (contextualMessage);
    }

    protected override void Interact () {
        base.Interact ();
        ShowInteractMessage ();
    }

    void OnValidate () {
        interactMessage = "#set from script#";
    }
}