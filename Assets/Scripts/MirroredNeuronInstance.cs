using UnityEngine;
using Neuron;

public class MirroredNeuronInstance : NeuronTransformsInstance
{
    private static readonly Quaternion mirrorQ = Quaternion.Euler(0, 180, 0); // 180° around Y axis mirrors across X

    void Update()
    {
        // Do NOT call base.Update() -- we want to intercept and mirror the mocap data ourselves
        if (boundActor != null && boundTransforms && motionUpdateMethod == UpdateMethod.Normal)
        {
            ApplyMirroredMotion(boundActor);
        }
    }

    private void ApplyMirroredMotion(NeuronActor actor)
    {
        if (!actor.HsReceivedData)
            return;

        Transform[] transforms = GetTransforms();
        if (transforms == null) return;

        for (int i = 0; i < (int)NeuronBones.NumOfBones && i < transforms.Length; ++i)
        {
            if (transforms[i] == null)
                continue;

            int srcIndex = GetMirroredBoneIndex(i); // Get the source bone (opposite side for L/R, self for center)
            if (srcIndex == -1) continue;

            // Get mocap data for the source bone
            Vector3 srcPos = actor.GetReceivedPosition((NeuronBones)srcIndex);
            Quaternion srcRot = Quaternion.Euler(actor.GetReceivedRotation((NeuronBones)srcIndex));

            // Mirror position and rotation in local space
            Vector3 mirroredPos = srcPos;
            mirroredPos.x = -mirroredPos.x;
            Quaternion mirroredRot = mirrorQ * srcRot * mirrorQ;

            // Only move if allowed
            bool enableNodeMove = actor.GetHasPosition((NeuronBones)srcIndex);
            enableNodeMove &= (!disableBoneMovement[i]);

            if (!enableFingerMove)
            {
                if (i >= (int)NeuronBones.RightHand && i <= (int)NeuronBones.RightHandPinky3)
                    enableNodeMove = false;
                if (i >= (int)NeuronBones.LeftHand && i <= (int)NeuronBones.LeftHandPinky3)
                    enableNodeMove = false;
            }

            if (enableNodeMove)
            {
                ApplyPosition(transforms, (NeuronBones)i, mirroredPos);
            }

            ApplyRotation(transforms, (NeuronBones)i, mirroredRot);
        }
    }

    private void ApplyPosition(Transform[] transforms, NeuronBones bone, Vector3 position)
    {
        Transform t = transforms[(int)bone];
        if (t != null)
        {
            Vector3 lossyScale = t.parent == null ? Vector3.one : t.parent.lossyScale;
            position.Scale(new Vector3(1.0f / lossyScale.x, 1.0f / lossyScale.y, 1.0f / lossyScale.z));
            if (!float.IsNaN(position.x) && !float.IsNaN(position.y) && !float.IsNaN(position.z))
            {
                t.localPosition = position;
            }
        }
    }

    private void ApplyRotation(Transform[] transforms, NeuronBones bone, Quaternion rotation)
    {
        Transform t = transforms[(int)bone];
        if (t != null)
        {
            if (!float.IsNaN(rotation.x) && !float.IsNaN(rotation.y) && !float.IsNaN(rotation.z) && !float.IsNaN(rotation.w))
            {
                t.localRotation = rotation;
            }
        }
    }

    // For mirroring, swap left/right bones, otherwise return self
    private int GetMirroredBoneIndex(int boneIndex)
    {
        switch ((NeuronBones)boneIndex)
        {
            case NeuronBones.RightArm: return (int)NeuronBones.LeftArm;
            case NeuronBones.LeftArm: return (int)NeuronBones.RightArm;
            case NeuronBones.RightForeArm: return (int)NeuronBones.LeftForeArm;
            case NeuronBones.LeftForeArm: return (int)NeuronBones.RightForeArm;
            case NeuronBones.RightHand: return (int)NeuronBones.LeftHand;
            case NeuronBones.LeftHand: return (int)NeuronBones.RightHand;
            case NeuronBones.RightUpLeg: return (int)NeuronBones.LeftUpLeg;
            case NeuronBones.LeftUpLeg: return (int)NeuronBones.RightUpLeg;
            case NeuronBones.RightLeg: return (int)NeuronBones.LeftLeg;
            case NeuronBones.LeftLeg: return (int)NeuronBones.RightLeg;
            case NeuronBones.RightFoot: return (int)NeuronBones.LeftFoot;
            case NeuronBones.LeftFoot: return (int)NeuronBones.RightFoot;
            case NeuronBones.RightHandThumb1: return (int)NeuronBones.LeftHandThumb1;
            case NeuronBones.LeftHandThumb1: return (int)NeuronBones.RightHandThumb1;
            case NeuronBones.RightHandIndex1: return (int)NeuronBones.LeftHandIndex1;
            case NeuronBones.LeftHandIndex1: return (int)NeuronBones.RightHandIndex1;
            case NeuronBones.RightHandMiddle1: return (int)NeuronBones.LeftHandMiddle1;
            case NeuronBones.LeftHandMiddle1: return (int)NeuronBones.RightHandMiddle1;
            case NeuronBones.RightHandRing1: return (int)NeuronBones.LeftHandRing1;
            case NeuronBones.LeftHandRing1: return (int)NeuronBones.RightHandRing1;
            case NeuronBones.RightHandPinky1: return (int)NeuronBones.LeftHandPinky1;
            case NeuronBones.LeftHandPinky1: return (int)NeuronBones.RightHandPinky1;
            case NeuronBones.Hips:
            case NeuronBones.Spine:
            case NeuronBones.Spine1:
            case NeuronBones.Neck:
            case NeuronBones.Head:
                return boneIndex;
            default:
                return boneIndex; // For bones not explicitly swapped, just mirror self
        }
    }
} 