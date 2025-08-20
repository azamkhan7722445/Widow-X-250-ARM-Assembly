using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.XR.Shared.Utils
{
    public static class TransformManipulations
    {
        // Return the position and rotation of a referential referenceTransform so that a offsetedTransform already placed properly will have the required positionOffset/rotationOffset
        public static (Vector3 newReferencePosition, Quaternion newReferencerotation) ReferentialPositionToRespectChildPositionOffset(
            Transform referenceTransform, 
            Vector3 offsetedTransformPosition, Quaternion offsetedTransformRotation, 
            Vector3 positionOffset, Quaternion rotationOffset,
            bool acceptLossyScale = false,
            Vector3? forcedScale = null
            )
        {
            var rotation = offsetedTransformRotation * Quaternion.Inverse(rotationOffset);
            // We do not apply the rotation to the transform right now, so to use the rotated transform, we can't rely on it and have to use a matrix to emulate in advance the new transform position
            Vector3 scale;
            if (forcedScale != null)
            {
                scale = forcedScale.GetValueOrDefault();
            }
            else if (referenceTransform.parent != null)
            {
                if (acceptLossyScale)
                {
                    scale = referenceTransform.lossyScale;
                } 
                else
                {
                    throw new System.Exception("[ReferentialPositionToRespectChildPositionOffset] Lossy scale not accepted while the reference transform has a parent");
                }
            }
            else
            {
                scale = referenceTransform.localScale;
            }
            var referenceTransformMatrix = Matrix4x4.TRS(referenceTransform.position, rotation, scale);
            // If the transform was already rotated, it would be equivalent to Equivalent to:
            //     var offsetInRotatedReference = referenceTransform.TransformPoint(positionOffset);
            var offsetedInRotatedReference = referenceTransformMatrix.MultiplyPoint(positionOffset);
            var position = offsetedTransformPosition - (offsetedInRotatedReference - referenceTransform.transform.position);
            var movedReferenceTransformMatrix = Matrix4x4.TRS(position, rotation, referenceTransform.localScale);
            var appliedOffsetInFixedRef = movedReferenceTransformMatrix.MultiplyPoint(positionOffset);
            return (position, rotation);
        }

        public static (Vector3 newReferencePosition, Quaternion newReferencerotation) ReferentialPositionToRespectChildPositionUnscaledOffset(
            Transform referenceTransform,
            Vector3 offsetedTransformPosition, Quaternion offsetedTransformRotation,
            Vector3 positionOffset, Quaternion rotationOffset
        )
        {
            return ReferentialPositionToRespectChildPositionOffset(referenceTransform, offsetedTransformPosition, offsetedTransformRotation, positionOffset, rotationOffset, forcedScale: Vector3.one);
        }

        // Return 
        public static (Vector3 offset, Quaternion rotationOffset) UnscaledOffset(Transform referenceTransform, Transform transformToOffset)
        {
            // Equivalent to "offset = referenceTransform.InverseTransformPoint(transformToOffset.position)" when the referenceTransform scale is Vector3.one, as well as the one of its parents
            var referenceTransformMatrix = Matrix4x4.TRS(referenceTransform.position, referenceTransform.rotation, Vector3.one);
            var offset = referenceTransformMatrix.inverse.MultiplyPoint(transformToOffset.position);

            var rotationOffset = Quaternion.Inverse(referenceTransform.transform.rotation) * transformToOffset.rotation;
            return (offset, rotationOffset);
        }

        public static (Vector3 position, Quaternion rotation) ApplyUnscaledOffset(Transform referenceTransform, Vector3 offset, Quaternion rotationOffset)
        {
            var rotation = referenceTransform.transform.rotation * rotationOffset;
            var referenceTransformMatrix = Matrix4x4.TRS(referenceTransform.position, referenceTransform.rotation, Vector3.one);
            var position = referenceTransformMatrix.MultiplyPoint(offset);
            return (position, rotation);
        }
    }
}
