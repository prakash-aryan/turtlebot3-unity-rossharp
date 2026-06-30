using UnityEngine;
using RosSharp.RosBridgeClient;
using Geometry = RosSharp.RosBridgeClient.MessageTypes.Geometry;

// Subscribes to /cmd_vel (geometry_msgs/Twist) and drives the kinematic robot by
// integrating the base pose. ROS angular.z is CCW; Unity yaw is CW, so it's negated.
// Defaults are TurtleBot3 Burger velocity limits.
public class DiffDriveController : MonoBehaviour
{
    public string CmdVelTopic = "cmd_vel";
    public float MaxLinear = 0.22f;
    public float MaxAngular = 2.84f;
    public float CmdTimeout = 0.5f;

    private RosConnector connector;
    private volatile float cmdLinear;
    private volatile float cmdAngular;
    private volatile bool gotCmd;
    private float lastCmdTime;
    private bool subscribed;

    private void Start()
    {
        connector = GetComponentInParent<RosConnector>();
    }

    private void FixedUpdate()
    {
        if (connector == null || connector.RosSocket == null) return;
        if (!subscribed)
        {
            connector.RosSocket.Subscribe<Geometry.Twist>(CmdVelTopic, OnCmdVel);
            subscribed = true;
        }
        if (gotCmd) { lastCmdTime = Time.time; gotCmd = false; }

        float lin = cmdLinear, ang = cmdAngular;
        if (Time.time - lastCmdTime > CmdTimeout) { lin = 0f; ang = 0f; }
        lin = Mathf.Clamp(lin, -MaxLinear, MaxLinear);
        ang = Mathf.Clamp(ang, -MaxAngular, MaxAngular);

        float dt = Time.fixedDeltaTime;
        transform.Rotate(0f, -ang * Mathf.Rad2Deg * dt, 0f, Space.World);
        transform.position += transform.forward * (lin * dt);
    }

    private void OnCmdVel(Geometry.Twist msg)
    {
        cmdLinear = (float)msg.linear.x;
        cmdAngular = (float)msg.angular.z;
        gotCmd = true;
    }
}
