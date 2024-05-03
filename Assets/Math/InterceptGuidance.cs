using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static StaticMath;

// https://github.com/Woreira/Unity-Proportional-Navigation-Collection

/*
MIT License

Copyright (c) 2021 Woreira

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */

public static class InterceptGuidance
{
    /// <summary>
    /// Line Of Sight Proportional Navigation. 
    /// The missile aims for the future position of the target, taking into account the target's velocity. 
    /// It calculates the angle between the missile's velocity and the line of sight vector, 
    /// and then uses the pValue variable to adjust the missile's velocity in the direction of the line of sight vector.
    /// </summary>
    public static Quaternion LOSPN(
        float navigationCoefficient, GameObject target, Vector3 targetVelocity,
        Vector3 ownPosition, Quaternion ownRotation, float ownSpeed, float turnRate)
    {
        float navigationTime = (target.transform.position - ownPosition).magnitude / ownSpeed;

        Vector3 los = (target.transform.position + targetVelocity * navigationTime) - ownPosition;

        float angle = Vector3.Angle(targetVelocity, los);
        Vector3 adjustment = navigationCoefficient * angle * los.normalized;

        var target_rotation = Quaternion.LookRotation(adjustment);
        return Quaternion.RotateTowards(ownRotation, target_rotation, turnRate);
    }

    /// <summary>
    /// It is the simplest implementation of PN that I could come up with. 
    /// It estimates a flight time to target, then uses that to estimate the expected position of the target.
    /// </summary>
    public static Quaternion SimplifiedPN(
        float navigationCoefficient, GameObject target, Vector3 targetVelocity,
        Vector3 ownPosition, Quaternion ownRotation, float ownSpeed, float turnRate)
    {
        Vector3 los = target.transform.position - ownPosition;

        float navigationTime = los.magnitude / ownSpeed;

        Vector3 targetRelativeInterceptPosition = los + (targetVelocity * navigationTime);

        Vector3 desiredHeading = targetRelativeInterceptPosition.normalized;

        targetRelativeInterceptPosition *= navigationCoefficient;   //multiply the relative intercept pos so the missile will lead a bit more

        var target_rotation = Quaternion.LookRotation((target.transform.position + targetRelativeInterceptPosition) - ownPosition);
        return Quaternion.RotateTowards(ownRotation, target_rotation, turnRate);
    }

    /// <summary>
    /// This is the most precise and robust PN. 
    /// It uses the GetInterceptDirection(...) method to solve the interception triangle 
    /// (via cossine law and quadratics).
    /// 
    /// > Quadratic is by far the most accurate guidance system, 
    /// > makes the missile travel in a complete straight line to the intercept point, 
    /// > given that there is a possible intercept point
    /// 
    /// GetInterceptDirection(...): Uses SolveQuadratic(...) 
    /// to solve the intercept traingle and outs the direction when possible
    /// 
    /// > Note: These methods can also be used to solve for intercept bullets 
    /// > (i.e. firing a bullet at a moving target), hence I preferred to detach then
    /// 
    /// </summary>
    public static Quaternion QuadraticPN(
        float navigationCoefficient, GameObject target, Vector3 targetVelocity,
        Vector3 ownPosition, Quaternion ownRotation, float ownSpeed, float turnRate)
    {
        Vector3 direction;
        Quaternion target_rotation = Quaternion.identity;

        if (GetInterceptDirection(ownPosition, target.transform.position, ownSpeed, targetVelocity, out direction))
        {
            target_rotation = Quaternion.LookRotation(direction);
        }
        else
        {
            //well, I guess we cant intercept then
        }

        return Quaternion.RotateTowards(ownRotation, target_rotation, turnRate * Time.deltaTime);
    }

    public static bool GetInterceptDirection(Vector3 origin, Vector3 targetPosition, float missileSpeed, Vector3 targetVelocity, out Vector3 result)
    {

        var los = origin - targetPosition;
        var distance = los.magnitude;
        var alpha = Vector3.Angle(los, targetVelocity) * Mathf.Deg2Rad;
        var vt = targetVelocity.magnitude;
        var vRatio = vt / missileSpeed;

        //solve the triangle, using cossine law
        if (SolveQuadratic(1 - (vRatio * vRatio), 2 * vRatio * distance * Mathf.Cos(alpha), -distance * distance, out var root1, out var root2) == 0)
        {
            result = Vector3.zero;
            return false;   //no intercept solution possible!
        }

        var interceptVectorMagnitude = Mathf.Max(root1, root2);
        var time = interceptVectorMagnitude / missileSpeed;
        var estimatedPos = targetPosition + targetVelocity * time;
        result = (estimatedPos - origin).normalized;

        return true;
    }

    public static int SolveQuadratic(float a, float b, float c, out float root1, out float root2)
    {

        var discriminant = b * b - 4 * a * c;

        if (discriminant < 0)
        {
            root1 = Mathf.Infinity;
            root2 = -root1;
            return 0;
        }

        root1 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
        root2 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);

        return discriminant > 0 ? 2 : 1;
    }
}
