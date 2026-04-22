using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectPrecipicePT
{
    public interface IActionable
    {
        void OnPrimaryActionStarted();
        void OnPrimaryActionPerformed();
        void OnPrimaryActionCanceled();

        void OnSecondaryActionStarted();
        void OnSecondaryActionPerformed();
        void OnSecondaryActionCanceled();
    }
}
