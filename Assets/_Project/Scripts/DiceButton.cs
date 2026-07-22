using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class DiceButton : MonoBehaviour
{
    [SerializeField] private UnityEvent Clicked;

    private Camera targetCamera;

    private void Awake()
    {
        targetCamera = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Ray ray = targetCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider == GetComponent<Collider>())
            {
                Clicked?.Invoke();
            }
        }
    }

    private void OnMouseDown()
    {
        Clicked?.Invoke();
    }

}