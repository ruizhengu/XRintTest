# ActionBuilder - Fluent API for XR Interactions

The ActionBuilder provides a fluent API pattern for chaining XR interaction actions, allowing you to create readable and maintainable test sequences.

## Basic Usage

Instead of writing separate coroutine calls, you can chain actions together:

```csharp
// Old way
yield return GrabAndHold(1.0f);
yield return MoveUp(0.5f);

// New way with ActionBuilder
var action = new ActionBuilder(this);
action.GrabHold(1.0f)
      .MoveUp(0.5f);
yield return action.Execute();
```

## Available Actions

### Grab Actions
- `GrabStart()` - Start grabbing (press grab key)
- `GrabEnd()` - End grabbing (release grab key)
- `GrabHold(duration)` - Grab and hold for a duration

### Movement Actions
- `MoveStart(key)` - Start movement in a direction
- `MoveEnd(key)` - End movement in a direction
- `MoveHold(key, duration)` - Move and hold in a direction

### Directional Movement Helpers
- `MoveUp(duration)` - Move up (E key)
- `MoveDown(duration)` - Move down (Q key)
- `MoveForward(duration)` - Move forward (W key)
- `MoveBackward(duration)` - Move backward (S key)
- `MoveLeft(duration)` - Move left (A key)
- `MoveRight(duration)` - Move right (D key)

### Combined Actions
- `GrabAndMove(key, duration)` - Grab and move simultaneously
- `GrabAndMoveContinuous(key, duration)` - Grab first, then move
- `GrabAndMoveUp(duration)` - Grab and move up
- `GrabAndMoveDown(duration)` - Grab and move down
- `GrabAndMoveForward(duration)` - Grab and move forward
- `GrabAndMoveBackward(duration)` - Grab and move backward
- `GrabAndMoveLeft(duration)` - Grab and move left
- `GrabAndMoveRight(duration)` - Grab and move right

### Utility Actions
- `Wait(duration)` - Wait for a specified duration
- `PressKey(key, duration)` - Execute a key press
- `Custom(action, description)` - Add a custom action to the chain

## Examples

### Basic Action Chain
```csharp
var action = new ActionBuilder(this);
action.GrabHold(1.0f)
      .MoveUp(0.5f);
yield return action.Execute();
```

### Complex Action Chain
```csharp
var action = new ActionBuilder(this);
action.GrabStart()           // Start grabbing
      .Wait(0.2f)           // Wait a bit
      .MoveUp(0.5f)         // Move up while grabbing
      .Wait(0.1f)           // Brief pause
      .MoveRight(0.3f)      // Move right
      .Wait(0.1f)           // Brief pause
      .MoveDown(0.3f)       // Move down
      .GrabEnd();           // Release grab

yield return action.Execute();
```

### Using Combined Actions
```csharp
var action = new ActionBuilder(this);
action.GrabAndMoveUp(1.0f)      // Grab and move up simultaneously
      .Wait(0.5f)               // Wait
      .GrabAndMoveForward(0.8f) // Grab and move forward
      .Wait(0.3f)               // Wait
      .GrabAndMoveRight(0.6f);  // Grab and move right

yield return action.Execute();
```

### Custom Actions
```csharp
var action = new ActionBuilder(this);
action.GrabStart()
      .Custom(() => NavigateToObject(origin.transform, cubeObj.transform), "Navigate to cube")
      .MoveUp(0.5f)
      .Custom(() => PressKey(Key.Space), "Press space")
      .GrabEnd();

yield return action.Execute();
```

### Using ActionBuilderHelper
```csharp
// Inline execution without creating a variable
yield return ActionBuilderHelper.ExecuteChain(builder => 
    builder.GrabHold(1.0f)
           .MoveUp(0.5f)
           .MoveRight(0.3f), 
    this);
```

### Reusable Action Chains
```csharp
// Create a reusable action chain
var grabAndLift = new ActionBuilder(this);
grabAndLift.GrabStart()
           .Wait(0.1f)
           .MoveUp(0.8f)
           .GrabEnd();

// Execute the same chain multiple times
yield return grabAndLift.Execute();
yield return new WaitForSeconds(1.0f);
yield return grabAndLift.Execute();
```

### Conditional Action Chains
```csharp
var action = new ActionBuilder(this);
action.GrabStart();

// Add different movements based on conditions
if (cubeObj.transform.position.y < 1.0f)
{
    action.MoveUp(0.5f);
}
else
{
    action.MoveDown(0.3f);
}

action.GrabEnd();
yield return action.Execute();
```

## Advanced Features

### Async Execution
```csharp
var action = new ActionBuilder(this);
action.GrabHold(1.0f).MoveUp(0.5f);

// Execute asynchronously (non-blocking)
action.ExecuteAsync(this);
```

### Clearing Actions
```csharp
var action = new ActionBuilder(this);
action.GrabHold(1.0f).MoveUp(0.5f);

// Clear all actions
action.Clear();

// Add new actions
action.GrabStart().GrabEnd();
yield return action.Execute();
```

### Action Count
```csharp
var action = new ActionBuilder(this);
action.GrabHold(1.0f).MoveUp(0.5f).MoveRight(0.3f);

Debug.Log($"Action chain contains {action.Count} actions"); // Output: 3
```

## Benefits

1. **Readability**: Action chains are self-documenting and easy to understand
2. **Maintainability**: Easy to modify action sequences by adding/removing steps
3. **Reusability**: Action chains can be reused across different tests
4. **Flexibility**: Support for custom actions and conditional logic
5. **Debugging**: Built-in logging shows which actions are being executed

## Integration with Existing Code

The ActionBuilder is designed to work seamlessly with the existing `TestLib` methods. All the underlying functionality remains the same - the ActionBuilder just provides a more convenient way to chain actions together.

## Performance Considerations

- ActionBuilder stores actions in memory until execution
- For very long action chains, consider breaking them into smaller, reusable pieces
- The execution is sequential, so total time is the sum of all action durations 
