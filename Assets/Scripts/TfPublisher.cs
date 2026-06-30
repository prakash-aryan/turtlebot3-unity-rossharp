using System.Collections.Generic;
using UnityEngine;
using RosSharp;                       // Unity2Ros extensions
using RosSharp.RosBridgeClient;
using Tf2 = RosSharp.RosBridgeClient.MessageTypes.Tf2;
using Geometry = RosSharp.RosBridgeClient.MessageTypes.Geometry;
using Std = RosSharp.RosBridgeClient.MessageTypes.Std;

// Publishes /tf: odom -> base_footprint (dynamic, from the robot's world pose,
// odom == start origin) plus the static link frames from the URDF hierarchy.
// SLAM owns map -> odom, so that is intentionally NOT published here.
public class TfPublisher : MonoBehaviour
{
    public string Topic = "tf";
    public string OdomFrame = "odom";
    public float PublishRateHz = 30f;

    private RosConnector connector;
    private string publicationId;
    private float nextPublish;
    private bool advertised;
    private Transform baseFootprint;

    private struct Link { public string parent; public Transform t; }
    private readonly List<Link> links = new List<Link>();

    private void Start()
    {
        connector = GetComponentInParent<RosConnector>();
        baseFootprint = FindDeep(transform, "base_footprint");
        AddLink("base_footprint", "base_link");
        AddLink("base_link", "base_scan");
        AddLink("base_link", "camera_link");
        AddLink("base_link", "wheel_left_link");
        AddLink("base_link", "wheel_right_link");
        AddLink("base_link", "caster_back_link");
        AddLink("base_link", "imu_link");
    }

    private void AddLink(string parent, string child)
    {
        var t = FindDeep(transform, child);
        if (t != null) links.Add(new Link { parent = parent, t = t });
    }

    private void Update()
    {
        if (connector == null || connector.RosSocket == null || baseFootprint == null) return;
        if (!advertised)
        {
            publicationId = connector.RosSocket.Advertise<Tf2.TFMessage>(Topic);
            advertised = true;
        }
        if (Time.time < nextPublish) return;
        nextPublish = Time.time + 1f / PublishRateHz;

        var list = new List<Geometry.TransformStamped>();
        list.Add(Make(OdomFrame, "base_footprint", baseFootprint.position, baseFootprint.rotation));
        foreach (var l in links)
            list.Add(Make(l.parent, l.t.name, l.t.localPosition, l.t.localRotation));

        connector.RosSocket.Publish(publicationId, new Tf2.TFMessage { transforms = list.ToArray() });
    }

    private Geometry.TransformStamped Make(string parent, string child, Vector3 pos, Quaternion rot)
    {
        Vector3 p = pos.Unity2Ros();
        Quaternion q = rot.Unity2Ros();
        var ts = new Geometry.TransformStamped
        {
            header = new Std.Header { frame_id = parent },
            child_frame_id = child,
            transform = new Geometry.Transform
            {
                translation = new Geometry.Vector3 { x = p.x, y = p.y, z = p.z },
                rotation = new Geometry.Quaternion { x = q.x, y = q.y, z = q.z, w = q.w }
            }
        };
        SimTime.Stamp(ts.header);
        return ts;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform c in root)
        {
            var r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }
}
