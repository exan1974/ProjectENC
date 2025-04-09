#if VFX_HAS_PHYSICS
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/Capsule Collider Binder")]
    [VFXBinder("Collider/Capsule")]
    public class VFXCapsuleBinder : VFXSpaceableBinder
    {
        // This exposed property will be used as a prefix for the bound data.
        // For example, if set to "Capsule", the binder will set properties named "Capsule_start", "Capsule_end", and "Capsule_radius".
        public string Property
        {
            get { return (string)m_Property; }
            set { m_Property = value; UpdateSubProperties(); }
        }

        [VFXPropertyBinding("UnityEditor.VFX.Capsule", "UnityEditor.VFX.TCapsule"), SerializeField]
        protected ExposedProperty m_Property = "Capsule";

        // A list of capsule colliders to bind (set 13 elements here in the Inspector).
        public List<CapsuleCollider> Targets = new List<CapsuleCollider>();

        // The names of the sub-properties that will be set on the VisualEffect.
        private ExposedProperty m_Starts;
        private ExposedProperty m_Ends;
        private ExposedProperty m_Radius;

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateSubProperties();
        }

        void OnValidate()
        {
            UpdateSubProperties();
        }

        void UpdateSubProperties()
        {
            // Create sub-properties based on the main property.
            m_Starts = m_Property + "_start";
            m_Ends = m_Property + "_end";
            m_Radius = m_Property + "_radius";
        }

        public override bool IsValid(VisualEffect component)
        {
            // This binder is valid if at least one collider is assigned and if the VFX Graph has the required properties.
            return Targets != null && Targets.Count > 0 &&
                   component.HasVector3(m_Starts) &&
                   component.HasVector3(m_Ends) &&
                   component.HasFloat(m_Radius);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            // Create lists to accumulate the capsule properties.
            List<Vector3> starts = new List<Vector3>();
            List<Vector3> ends = new List<Vector3>();
            List<float> radii = new List<float>();

            // Process each capsule collider
            foreach (var capsule in Targets)
            {
                if (capsule == null)
                    continue;

                // Obtain the VFX-space transformation for this collider's transform.
                Vector3 transformCenter;
                Vector3 transformScale;
                // The first parameter here (m_Starts) is arbitrary—what matters is the target transform.
                ApplySpaceTS(component, m_Starts, capsule.transform, out transformCenter, out transformScale);

                // Compute the VFX-space center.
                Vector3 worldCenter = transformCenter + capsule.center;

                // Determine the axis (the "elongated" direction) of the capsule based on its 'direction' setting.
                float heightScale = 1f;
                float radiusScale = 1f;
                Vector3 localAxis = Vector3.up; // default axis (Y-axis)
                switch (capsule.direction)
                {
                    case 0: // X-axis
                        heightScale = transformScale.x;
                        radiusScale = Mathf.Max(transformScale.y, transformScale.z);
                        localAxis = Vector3.right;
                        break;
                    case 1: // Y-axis
                        heightScale = transformScale.y;
                        radiusScale = Mathf.Max(transformScale.x, transformScale.z);
                        localAxis = Vector3.up;
                        break;
                    case 2: // Z-axis
                        heightScale = transformScale.z;
                        radiusScale = Mathf.Max(transformScale.x, transformScale.y);
                        localAxis = Vector3.forward;
                        break;
                }

                // Compute the effective (scaled) radius.
                float effectiveRadius = capsule.radius * radiusScale;
                // Compute half the height (scaled along the axis).
                float effectiveHalfHeight = (capsule.height * heightScale) / 2f;
                // The offset from the center to the spherical cap centers.
                float halfLine = Mathf.Max(0f, effectiveHalfHeight - effectiveRadius);

                // Convert the local axis into world (or VFX-space) by using the collider's transform direction.
                Vector3 worldAxis = capsule.transform.TransformDirection(localAxis).normalized;

                // Calculate the two endpoints.
                Vector3 start = worldCenter + worldAxis * halfLine;
                Vector3 end   = worldCenter - worldAxis * halfLine;

                starts.Add(start);
                ends.Add(end);
                radii.Add(effectiveRadius);
            }

            // Bind the arrays on the VisualEffect.
            component.SetVector3Array(m_Starts, starts);
            component.SetVector3Array(m_Ends, ends);
            component.SetFloatArray(m_Radius, radii);
        }

        public override string ToString()
        {
            string targetsStr = Targets != null
                ? string.Join(", ", Targets.ConvertAll(x => x != null ? x.name : "null").ToArray())
                : "(none)";
            return string.Format("Capsule : '{0}' -> [{1}]", m_Property, targetsStr);
        }
    }
}
#endif
