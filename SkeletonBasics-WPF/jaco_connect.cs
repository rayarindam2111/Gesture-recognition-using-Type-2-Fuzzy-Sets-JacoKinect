using System;
using Kinova.API.Jaco; 
using Kinova.DLL.SafeGate;
using Kinova.DLL.Data.Jaco.Config;
using Kinova.DLL.Data.Util;
using Kinova.DLL.Data.Jaco.Control;
using Kinova.DLL.Data.Jaco;
using Microsoft.Samples.Kinect.SkeletonBasics;


public class jaco_connect
{
    Log Logger;
    CJacoArm Jaco;
    CPointsTrajectory m_PointsTrajectory;
    CTrajectoryInfo pointTraj;
    public jaco_connect(Log x)
    {
        Logger = x;
    }
    public CJacoArm init()
    {
        const string MyValidPassword = "C6H12O6h2so4";

        pointTraj = new CTrajectoryInfo();

        try
        {
            Jaco = new CJacoArm(Crypto.GetInstance().Encrypt(MyValidPassword));
            Jaco.ControlManager.StartControlAPI();
        }
        catch (Exception ex) 
        {
            Logger.addLog("Error initializing Jaco!\n" + ex.Message , true);
            return Jaco;
        }

        if (Jaco.JacoIsReady())
        {
            CClientConfigurations config = new CClientConfigurations();

            config.ClientName = "Arup";
            config.MaxAngularSpeed = 0.2f;
            config.Organization = "SV";
            Jaco.ConfigurationsManager.SetClientConfigurations(config);

            Jaco.ControlManager.EraseTrajectories();

            Logger.addLog("Jaco initialized and ready");
            return Jaco;
        }
        else
        {
            Logger.addLog("Jaco initialized but not ready" , true);
            return Jaco;   
        }
             
    }

    //used to clear existing trajectory
    void TrajClear()
    {
        m_PointsTrajectory = new CPointsTrajectory();
    }

    //send trajectory to jaco
    public string sendTraj()
    {
        try
        {
            if (Jaco.JacoIsReady())
            {
                TrajClear();
                m_PointsTrajectory.Add(pointTraj);
                Jaco.ControlManager.SendBasicTrajectory(m_PointsTrajectory);
                //Delay(pointTraj, Jaco);
                Logger.addLog("Trajectory Completed (Angles set)");
                return "Angle set";
            }
            else
            {
                Logger.addLog("Jaco not ready - unable to send trajectory", true);
                return "Jaco not ready - unable to send trajectory";
            }          

        }
        catch (Exception)
        {
            Logger.addLog("Error in updating trajectory", true);
            return "Error in updating trajectory";
        }
  
    }


    public string sendMultiTraj(CPointsTrajectory traj)
    {
        try
        {
            if (Jaco.JacoIsReady())
            {
                Jaco.ControlManager.SendBasicTrajectory(traj);
                Logger.addLog("Trajectory Sent");
                return "Angle set";
            }
            else
            {
                Logger.addLog("Jaco not ready - unable to send trajectory", true);
                return "Jaco not ready - unable to send trajectory";
            }

        }
        catch (Exception)
        {
            Logger.addLog("Error in updating trajectory", true);
            return "Error in updating trajectory";
        }
    }

    //must call before every frame
    public void resetPoint()
    {
        try
        {
            if (Jaco.JacoIsReady())
            {
                CVectorAngle JointPosition = Jaco.ConfigurationsManager.GetJointPositions();
                pointTraj.UserPosition.AnglesJoints = JointPosition;
            }
            else
            {
                Logger.addLog("Jaco not ready - unable to get joint positions", true);
            }

        }
        catch (Exception)
        {
            Logger.addLog("Error in Jaco - unable to get joint positions", true);
        }
        
    }

    //update point co-ods in existing point
    public void UpdatePoint(int joint, double angle)
    {

        pointTraj.UserPosition.HandMode = CJacoStructures.HandMode.PositionMode;
        pointTraj.UserPosition.PositionType = CJacoStructures.PositionType.AngularPosition;
        //7 because mapping between Kinect JointType to Jaco Joint No.
        pointTraj.UserPosition.AnglesJoints.Angle[joint - 7] = (float)angle;
        
    }

    public void Delay(CTrajectoryInfo point, CJacoArm jArm)
    {
        float J1, J2, J3 /*, J4, J5, J6, F1, F2, F3*/;
        int k = 0;
        int jacoTimeout = 0; //max delay timeout for jaco to track arm
        CAngularInfo info;
        float thresh = 6;
        do
        {
            try
            {
                info = jArm.ControlManager.GetPositioningAngularInfo();
            }
            catch (Exception)
            { 
                Logger.addLog("Error in obtaining Jaco current angles", true);
                return;
            }
            
            J1 = Math.Abs(info.Joint1 - point.UserPosition.AnglesJoints.Angle[CVectorAngle.JOINT_1]);
            J2 = Math.Abs(info.Joint2 - point.UserPosition.AnglesJoints.Angle[CVectorAngle.JOINT_2]);
            J3 = Math.Abs(info.Joint3 - point.UserPosition.AnglesJoints.Angle[CVectorAngle.JOINT_3]);
           // J4 = Math.Abs(info.Joint4 - point.UserPosition.AnglesJoints.Angle[CVectorAngle.JOINT_4]);
           // J5 = Math.Abs(info.Joint5 - point.UserPosition.AnglesJoints.Angle[CVectorAngle.JOINT_5]);
           // J6 = Math.Abs(info.Joint6 - point.UserPosition.AnglesJoints.Angle[CVectorAngle.JOINT_6]);
           // F1 = Math.Abs(info.Finger1 - point.UserPosition.FingerPosition[0]);
           // F2 = Math.Abs(info.Finger2 - point.UserPosition.FingerPosition[1]);
           // F3 = Math.Abs(info.Finger3 - point.UserPosition.FingerPosition[2]);
            k++;
        }
        while (k < jacoTimeout && !((J1 < thresh) && (J2 < thresh) && (J3 < thresh)/* && (J4 < 2.00)) && (J5 < 2.00)) && (J6 < 2.00)*/));
        if (k >= jacoTimeout) Logger.addLog("Timeout of " + jacoTimeout.ToString() + " loops exceeded");
    }

}
