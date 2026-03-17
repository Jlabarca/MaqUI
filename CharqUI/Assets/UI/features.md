# Features and Usage

This document lists the main features of the UI and Event frameworks and provides code samples for their use.

## `UIFramework` Features

The `UIFramework` is the static manager for all UI views.

### View Management

You must register views with the framework before they can be used. This is typically done once at startup.

-   **`BindView<T>(T view, bool isHomeView = false)`**: Adds a view instance to the framework's registry. The `isHomeView` flag designates the default view for navigation.
-   **`UnbindView<T>(T view = null)`**: Removes a view from the registry, either by instance or by type.
-   **`GetView<T>()`**: Retrieves a registered view instance by its type.

**Example: Registering Views**
This is typically done in a scene's installer or manager script.

```csharp
// From: @Shared/UIFramework/Demo/Scripts/SceneInstaller.cs

internal sealed class SceneInstaller : MonoBehaviour
{
    [SerializeField] private DemoHomeView homeView;
    [SerializeField] private DemoPageView pageView;

    private void Start() {
        // Instantiate and bind the home view, marking it as the default
        UIFramework.BindView(Instantiate(homeView), true);

        // Instantiate and bind other views
        UIFramework.BindView(Instantiate(pageView));
    }
}
```

### Navigation

The framework controls the flow between different application screens.

-   **`Navigate(IBaseView view, DisplayOptions options = null)`**: Hides the current view and shows the specified one, pushing the new view onto the navigation history stack.
-   **`GoBack(DisplayOptions options = null)`**: Hides the current view and navigates to the previous view in the history stack.
-   **`GoHome(bool closeAll = false)`**: Navigates to the registered home view. If `closeAll` is true, the entire navigation history is cleared.

**Example: Navigating Between Views**

```csharp
// From: @OneUI/Scripts/Views/WelcomeView.cs

public class WelcomeView : BaseView
{
    [SerializeField] private Button GetStartedButton;

    public override void OnViewStart() {
        GetStartedButton.onClick.AddListener(OnStartClicked);
    }

    private void OnStartClicked() {
        // Hide this view
        HideView(new DisplayOptions { IsAnimated = true });
        
        // Find and show the HomeView
        UIFramework.GetView<HomeView>().ShowView(new DisplayOptions { IsAnimated = true });
    }
}
```

## `EventMessenger` Features

The `EventMessenger` provides a global, decoupled communication system.

### Publish/Subscribe

Any component can publish an event (with a data payload), and any other component can subscribe to receive it based on the payload type.

-   **`Subscribe<T>(Action<T> callback)`**: Listens for a payload of type `T`. The callback is invoked when a matching payload is published.
-   **`Unsubscribe<T>(Action<T> callback)`**: Removes a subscription.
-   **`Publish<T>(T payload)`**: Broadcasts a payload to all subscribers of type `T`.

### Payloads

Payloads are simple C# classes that define the data contract for an event. They can include data fields and `Action` callbacks for the subscriber to execute.

**Example: Requesting a Confirmation Dialog**
This example shows the `HomeView` requesting a confirmation dialog. The `PopupView` listens for this request and configures itself based on the payload data.

**1. Define the Payload**
```csharp
// From: @OneUI/Scripts/Payloads/ConfirmDialoguePayload.cs
public class ConfirmDialoguePayload : IPayload
{
    public string Title = "";
    public string Description = "";
    public Action OnConfirm;
    public Action OnCancel;
    public bool ShowCancelButton;
}
```

**2. Publisher: `HomeView`**
```csharp
// From: @OneUI/Scripts/Views/HomeView.cs
public class HomeView : BaseView
{
    [SerializeField] private Button QuitButton;

    private void BindGeneralButtons() {
        QuitButton.onClick.AddListener(() => {
            // Publish the payload with data and a callback
            EventMessenger.Main.Publish(new ConfirmDialoguePayload {
                Title = "Are you Sure?",
                Description = "Do you really want to quit?",
                OnConfirm = () => {
                    Debug.Log("Quit confirmed!");
                    Application.Quit();
                },
                ShowCancelButton = true
            });
        });
    }
}
```

**3. Subscriber: `PopupView`**
```csharp
// From: @OneUI/Scripts/Views/PopupView.cs
public class PopupView : BaseView
{
    [SerializeField] private TextMeshProUGUI Title;
    [SerializeField] private Button ApplyButton;

    public override void OnViewAwake() {
        SetAsGlobalView();
        // Subscribe to the event
        EventMessenger.Main.Subscribe<ConfirmDialoguePayload>(OnWindowRequested);
    }

    private void OnWindowRequested(ConfirmDialoguePayload payload) {
        // Configure the UI from the payload
        Title.SetText(payload.Title);
        
        // Wire up the callback from the payload to the button
        ApplyButton.onClick.RemoveAllListeners();
        ApplyButton.onClick.AddListener(() => {
            payload.OnConfirm?.Invoke();
            HideView(new DisplayOptions { IsAnimated = true });
        });

        // Show the popup
        ShowView(new DisplayOptions { IsAnimated = true });
    }
}
```

## Included `OneUI` Views & Payloads

The `OneUI` package provides a set of pre-built views that serve as excellent examples of the framework in action.

-   **Views**:
    -   `WelcomeView`: An initial landing screen.
    -   `HomeView`: The main application screen.
    -   `PopupView`: Handles `ConfirmDialoguePayload` for confirmation dialogs.
    -   `ErrorView`: Handles `ErrorPayload` to show error messages.
    -   `LoadingView`: Handles `LoadingPayload` to show a progress bar.
    -   `PreloaderView`: Handles `PreloaderPayload` for a simple loading spinner.
    -   `PromptView`: Handles `PromptPayload` to get text input from the user.
    -   `SearchView`: Handles `SearchPayload` to get a search query from the user.
-   **Payloads**: Each view above has a corresponding payload in the `DevsDaddy.OneUI.Scripts.Payloads` namespace that it listens for.
