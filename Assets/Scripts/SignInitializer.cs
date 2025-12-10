using UnityEngine;
using TMPro;

// This script runs on the SIGN PREFAB right after it is spawned 
// (or it can be run by the spawning script).
// Its primary role is to find the global UI components (from UIManager) 
// and inject them into the local SignInfo script on the same GameObject, 
// preventing null reference errors.
public class SignInitializer : MonoBehaviour
{
    private void Start()
    {
        InitializeSignReferences();
    }

    public void InitializeSignReferences()
    {
        // 1. Get the local SignInfo script that is currently missing references
        SignInfo signInfoComponent = GetComponent<SignInfo>();

        if (signInfoComponent == null)
        {
            Debug.LogError($"[Initialization Error] Sign {gameObject.name} is missing the required 'SignInfo' script. Cannot initialize UI references.");
            return;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogError($"[Initialization Error] UIManager.Instance is missing! Cannot inject global UI references into {gameObject.name}.");
            return;
        }

        // 2. Inject the global UIManager references into the local SignInfo component
        // Note: The UIManager.messagePanel/messageText fields are public, so we access them directly.
        signInfoComponent.displayPanel = UIManager.Instance.messagePanel;
        signInfoComponent.displayText = UIManager.Instance.messageText; 

        if (signInfoComponent.displayPanel != null && signInfoComponent.displayText != null)
        {
            Debug.Log($"SignInfo references successfully injected into {gameObject.name}. Sign is ready for reading.");
        }
        else
        {
            Debug.LogError($"[Setup Error] UIManager has null references for its Message Panel or Text! Check the UIManager Inspector.");
        }
        
        // This initializer has finished its job and can disable itself if it was placed 
        // directly on the sign prefab. If this script is run externally from 
        // another script, this line should be handled there.
        // this.enabled = false;
    }
}