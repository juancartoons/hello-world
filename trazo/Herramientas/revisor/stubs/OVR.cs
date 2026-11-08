using UnityEngine;
public class OVRCameraRig : MonoBehaviour
{
    public Transform centerEyeAnchor, leftHandAnchor, rightHandAnchor, trackingSpace, leftEyeAnchor, rightEyeAnchor, leftControllerAnchor, rightControllerAnchor;
}
public class OVRSkeleton : MonoBehaviour
{
    public enum BoneId { Invalid = -1, Hand_Start = 0, Hand_WristRoot = 0, Hand_ForearmStub, Hand_Thumb0, Hand_Thumb1, Hand_Thumb2, Hand_Thumb3, Hand_Index1, Hand_Index2, Hand_Index3, Hand_Middle1, Hand_Middle2, Hand_Middle3, Hand_Ring1, Hand_Ring2, Hand_Ring3, Hand_Pinky0, Hand_Pinky1, Hand_Pinky2, Hand_Pinky3, Hand_MaxSkinnable, Hand_ThumbTip = Hand_MaxSkinnable, Hand_IndexTip, Hand_MiddleTip, Hand_RingTip, Hand_PinkyTip, Hand_End, Max }
    public enum SkeletonType { None = -1, HandLeft, HandRight }
    public bool IsInitialized { get { return false; } }
    public bool IsDataValid { get { return false; } }
    public bool IsDataHighConfidence { get { return false; } }
    public System.Collections.Generic.IList<OVRBone> Bones { get { return null; } }
    public SkeletonType GetSkeletonType() { return SkeletonType.None; }
}
public class OVRBone { public OVRSkeleton.BoneId Id; public Transform Transform; }
public class OVRHand : MonoBehaviour
{
    public enum HandFinger { Thumb, Index, Middle, Ring, Pinky }
    public enum TrackingConfidence { Low, High }
    public bool IsTracked { get { return false; } }
    public bool IsDataValid { get { return false; } }
    public bool IsDataHighConfidence { get { return false; } }
    public bool IsPointerPoseValid { get { return false; } }
    public Transform PointerPose { get { return null; } }
    public float HandScale { get { return 1f; } }
    public bool GetFingerIsPinching(HandFinger f) { return false; }
    public float GetFingerPinchStrength(HandFinger f) { return 0f; }
    public TrackingConfidence GetFingerConfidence(HandFinger f) { return TrackingConfidence.Low; }
}
public class OVRPassthroughLayer : MonoBehaviour { public bool hidden; public float textureOpacity; }
public class OVRManager : MonoBehaviour
{
    public static OVRManager instance;
    public enum TrackingOrigin { EyeLevel, FloorLevel, Stage }
    public TrackingOrigin trackingOriginType;
    public bool isInsightPassthroughEnabled;
}
public class OVRScreenFade : MonoBehaviour { }

namespace RPStub
{
    public static class RenderPipeline
    {
        public class StandardRequest { public UnityEngine.RenderTexture destination; }
        public static bool SupportsRenderRequest<T>(UnityEngine.Camera c, T r) { return true; }
        public static void SubmitRenderRequest<T>(UnityEngine.Camera c, T r) { }
    }
}
