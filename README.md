### **InspectorReadOnlyTracker**  

#### **Description**  
InspectorReadOnlyTracker is a Unity Editor extension that tracks and displays real-time updates of fields and properties marked with the `[InspectorReadOnly]` attribute. It consists of two core scripts:  

1. **InspectorReadOnlyDrawer** – A custom property drawer that visually marks `[InspectorReadOnly]` fields in the inspector, preventing manual editing while showing live value updates.  
2. **InspectorReadOnlyInfo** – A tracking system that scans all MonoBehaviour components for `[InspectorReadOnly]` attributes, dynamically listing and updating their values in a dedicated inspector window.  

#### **Features**  
- **Live updates** – Tracks changes in `[InspectorReadOnly]` fields and highlights modifications in real-time.  
- **Automatic tracking** – Finds all relevant objects dynamically, handling creation and destruction.  
- **Optimized caching** – Uses reflection caching for better performance.  
- **Value change indication** – Displays "Value updated" messages for 2 seconds after a change.  

#### **Usage**  
1. Add the `[InspectorReadOnly]` attribute to any public or private field/property in a `MonoBehaviour` script.  
2. Attach the `InspectorReadOnlyInfo` script to an empty GameObject in the scene.  
3. Open the Inspector and expand the `InspectorReadOnlyInfo` component to see all tracked fields updating live.  

#### **Example**  
```csharp
public class TowerAI : MonoBehaviour
{
    [InspectorReadOnly] public int Health;
    [InspectorReadOnly] public bool IsUnderAttack;
}
```

This will make `Health` and `IsUnderAttack` appear in the `InspectorReadOnlyInfo` panel, with real-time updates as their values change.
