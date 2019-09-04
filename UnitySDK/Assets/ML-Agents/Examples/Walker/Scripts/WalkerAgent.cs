﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class WalkerAgent : Agent
{
    [Header("Obstacle")]
    float startDistance = -300;
    float obstacleInterval = 100;
    public Transform fencePrefab;
    //public Transform wallPrefab;
    float fenceMinHeight;
    float fenceMaxHeight;
    float wallMinWidth;
    float wallMaxWidth;
    List<Transform> fences;
    int obstaclesGenerated;

    [Header("Target To Walk Towards")]
    public Transform target;
    Vector3 dirToTarget;

    [Header("Specific to Walker")]
    public Transform hips;
    public Transform chest;
    public Transform spine;
    public Transform head;
    public Transform thighL;
    public Transform shinL;
    public Transform footL;
    public Transform thighR;
    public Transform shinR;
    public Transform footR;
    public Transform armL;
    public Transform forearmL;
    public Transform handL;
    public Transform armR;
    public Transform forearmR;
    public Transform handR;
    JointDriveController jdController;
    RayPerception3D rayPer;
    bool isNewDecisionStep;
    int currentDecisionStep;


    public override void InitializeAgent()
    {
        jdController = GetComponent<JointDriveController>();
        jdController.SetupBodyPart(hips);
        jdController.SetupBodyPart(chest);
        jdController.SetupBodyPart(spine);
        jdController.SetupBodyPart(head);
        jdController.SetupBodyPart(thighL);
        jdController.SetupBodyPart(shinL);
        jdController.SetupBodyPart(footL);
        jdController.SetupBodyPart(thighR);
        jdController.SetupBodyPart(shinR);
        jdController.SetupBodyPart(footR);
        jdController.SetupBodyPart(armL);
        jdController.SetupBodyPart(forearmL);
        jdController.SetupBodyPart(handL);
        jdController.SetupBodyPart(armR);
        jdController.SetupBodyPart(forearmR);
        jdController.SetupBodyPart(handR);
        rayPer = head.GetComponent<RayPerception3D>();

        obstaclesGenerated = 0;
        fences = new List<Transform>();
        fenceMinHeight = WalkerAcademy.fenceMinHeight;
        fenceMaxHeight = WalkerAcademy.fenceMaxHeight;
        while(obstaclesGenerated < 15)
            GenerateFence(fenceMinHeight, fenceMaxHeight);
    }

    /// <summary>
    /// Add relevant information on each body part to observations.
    /// </summary>
    public void CollectObservationBodyPart(BodyPart bp)
    {
        var rb = bp.rb;
        AddVectorObs(bp.groundContact.touchingGround ? 1 : 0); // Is this bp touching the ground
        AddVectorObs(rb.velocity);
        AddVectorObs(rb.angularVelocity);
        Vector3 localPosRelToHips = hips.InverseTransformPoint(rb.position);
        AddVectorObs(localPosRelToHips);

        if (bp.rb.transform != hips && bp.rb.transform != handL && bp.rb.transform != handR &&
            bp.rb.transform != footL && bp.rb.transform != footR && bp.rb.transform != head)
        {
            AddVectorObs(bp.currentXNormalizedRot);
            AddVectorObs(bp.currentYNormalizedRot);
            AddVectorObs(bp.currentZNormalizedRot);
            AddVectorObs(bp.currentStrength / jdController.maxJointForceLimit);
        }
    }

    /// <summary>
    /// Add ray perception and loop over body parts to add them to observation.
    /// </summary>
    public override void CollectObservations()
    {
        jdController.GetCurrentJointForces();

        AddVectorObs(dirToTarget.normalized);
        AddVectorObs(jdController.bodyPartsDict[hips].rb.position);
        AddVectorObs(hips.forward);
        AddVectorObs(hips.up);

        const float rayDistance = 50f;
        float[] rayAngles = { 90f, 70f, 110f};
        float[] rayAngles1 = { 95f, 75f, 115f};
        float[] rayAngles2 = { 85f, 65f, 105f};
        string[] detectableObjects = {"ground", "target", "fence"};
        AddVectorObs(rayPer.Perceive(rayDistance, rayAngles, detectableObjects, 0f, 0f));
        AddVectorObs(rayPer.Perceive(rayDistance, rayAngles1, detectableObjects, 0f, -30f));
        AddVectorObs(rayPer.Perceive(rayDistance, rayAngles2, detectableObjects, 0f, -15f));

        foreach (var bodyPart in jdController.bodyPartsDict.Values)
        {
            CollectObservationBodyPart(bodyPart);
        }
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        dirToTarget = target.position - jdController.bodyPartsDict[hips].rb.position;

        // Apply action to all relevant body parts.
        if (isNewDecisionStep)
        {
            var bpDict = jdController.bodyPartsDict;
            int i = -1;

            bpDict[chest].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], vectorAction[++i]);
            bpDict[spine].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], vectorAction[++i]);

            bpDict[thighL].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], 0);
            bpDict[thighR].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], 0);
            bpDict[shinL].SetJointTargetRotation(vectorAction[++i], 0, 0);
            bpDict[shinR].SetJointTargetRotation(vectorAction[++i], 0, 0);
            bpDict[footR].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], vectorAction[++i]);
            bpDict[footL].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], vectorAction[++i]);


            bpDict[armL].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], 0);
            bpDict[armR].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], 0);
            bpDict[forearmL].SetJointTargetRotation(vectorAction[++i], 0, 0);
            bpDict[forearmR].SetJointTargetRotation(vectorAction[++i], 0, 0);
            bpDict[head].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], 0);

            //update joint strength settings
            bpDict[chest].SetJointStrength(vectorAction[++i]);
            bpDict[spine].SetJointStrength(vectorAction[++i]);
            bpDict[head].SetJointStrength(vectorAction[++i]);
            bpDict[thighL].SetJointStrength(vectorAction[++i]);
            bpDict[shinL].SetJointStrength(vectorAction[++i]);
            bpDict[footL].SetJointStrength(vectorAction[++i]);
            bpDict[thighR].SetJointStrength(vectorAction[++i]);
            bpDict[shinR].SetJointStrength(vectorAction[++i]);
            bpDict[footR].SetJointStrength(vectorAction[++i]);
            bpDict[armL].SetJointStrength(vectorAction[++i]);
            bpDict[forearmL].SetJointStrength(vectorAction[++i]);
            bpDict[armR].SetJointStrength(vectorAction[++i]);
            bpDict[forearmR].SetJointStrength(vectorAction[++i]);
        }

        IncrementDecisionTimer();

        // Set reward for this step according to mixture of the following elements.
        // a. Velocity alignment with goal direction.
        // b. Rotation alignment with goal direction.
        // c. Encourage head height.
        // d. Discourage head movement.
        AddReward(
            + 0.03f * Vector3.Dot(dirToTarget.normalized, jdController.bodyPartsDict[hips].rb.velocity)
            + 0.01f * Vector3.Dot(dirToTarget.normalized, hips.forward)
            + 0.02f * (head.position.y - hips.position.y)
            - 0.01f * Vector3.Distance(jdController.bodyPartsDict[head].rb.velocity,
                jdController.bodyPartsDict[hips].rb.velocity)
        );
    }

    /// <summary>
    /// Only change the joint settings based on decision frequency.
    /// </summary>
    public void IncrementDecisionTimer()
    {
        if (currentDecisionStep == agentParameters.numberOfActionsBetweenDecisions ||
            agentParameters.numberOfActionsBetweenDecisions == 1)
        {
            currentDecisionStep = 1;
            isNewDecisionStep = true;
        }
        else
        {
            currentDecisionStep++;
            isNewDecisionStep = false;
        }
    }

    /// <summary>
    /// Loop over body parts and reset them to initial conditions, then delete old obstacles and generate new ones.
    /// </summary>
    public override void AgentReset()
    {
        if (dirToTarget != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(dirToTarget);
        }

        foreach (var bodyPart in jdController.bodyPartsDict.Values)
        {
            bodyPart.Reset(bodyPart);
        }

        isNewDecisionStep = true;
        currentDecisionStep = 1;

        fenceMinHeight = WalkerAcademy.fenceMinHeight;
        fenceMaxHeight = WalkerAcademy.fenceMaxHeight;
        int i = 0;
        fences.ForEach(delegate(Transform aux){
          UpdateFence(aux,fenceMinHeight,fenceMaxHeight,i);
          i++;
          });



    }

    private void GenerateFence(float minHeight, float maxHeight){
      Transform fence = null;
      fence = Instantiate(fencePrefab,
                          new Vector3(startDistance+(obstaclesGenerated*obstacleInterval)+Random.Range(-30,30),target.position.y,target.position.z),
                          Quaternion.LookRotation(dirToTarget));
      fence.gameObject.transform.localScale = new Vector3(0.25f,Random.Range(minHeight,maxHeight),100f);
      obstaclesGenerated++;
      fences.Add(fence);
    }

    private void UpdateFence(Transform fence, float minHeight, float maxHeight, int i){
      fence.gameObject.transform.position = new Vector3(startDistance+(i*obstacleInterval)+Random.Range(-30,30),target.position.y,target.position.z);
      fence.gameObject.transform.localScale = new Vector3(0.25f,Random.Range(minHeight,maxHeight),100f);
    }

    private void GenerateWall(float minWidth, float maxWidth){
      //TO DO
    }
}
