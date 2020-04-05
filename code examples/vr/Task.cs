
using System;
using System.Collections.Generic;
using UnityEngine;

public class Task
{
    public int ID { get; set; }
    public string Description { get; set; }
    public List<Switch> Switches { get; set; } // All of the switches that need to be activated in order for this task to be completed.
    public bool Completed { get; set; }
    public bool Called { get; set; }
    public bool OneShot { get; set; }

    public delegate void ActionPointer(bool completed);

    private List<ActionPointer> actions; // Called upon completion of task. Can be null if nothing needs to be called.

    public Task(string description, List<Switch> switches, List<ActionPointer> actions, bool oneShot)
    {
        Description = description;
        Switches = switches;
        this.actions = actions;
        Called = false;
        OneShot = oneShot;
    }
    
    public Task(string description, List<Switch> switches, List<ActionPointer> actions)
    {
        Description = description;
        Switches = switches;
        this.actions = actions;
        Called = false;
        OneShot = false;
    }

    public override string ToString()
    {
        var text = Description + "";
        foreach (var taskSwitch in Switches)
        {
            text += (" " + taskSwitch.name);
        }

        return text;
    }

    public bool CheckSwitches() // Check if all of the switches are on.
    {
        foreach (var switchToCheck in Switches)
        {
            if (!switchToCheck.On)
            {
                return false;
            }
            //Debug.Log(switchToCheck.gameObject.name + " IS ON");
        }
        
//        Debug.Log(Description + " COMPLETED!");
        return true;
    }

    public void UpdateCompletedVariable()
    {
        Completed = CheckSwitches();
        
    }

    public void CallCompletionActions() // Call a method after being completed.
    {
        if (!Called)
        {
            foreach (var action in actions)
            {
                action.Invoke(Completed);
            }

            if (OneShot && Completed)
            {
                Called = true;
            }
            
        }
        
    }

    public void AddCompletionAction(ActionPointer actionPointer)
    {
        actions.Add(actionPointer);
    }

    public void AddSwitch(Switch switchToAdd)
    {
        Switches.Add(switchToAdd);
    }
}
