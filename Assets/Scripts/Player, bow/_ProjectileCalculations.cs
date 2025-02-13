using UnityEngine;

[System.Serializable]
public class DrawTrajectory
{
    [SerializeField] LineRenderer trajectoryLr;
    [SerializeField] Gradient[] colorsTrajectory = new Gradient[2];
    [Sirenix.OdinInspector.ReadOnly] public bool showTrajectory;
    const int CONST_LinePoints = 10;
    const float CONST_TimeBetweenPoints = 0.1f;
    const float CONST_MinY = -10;

    public void Trajectory(bool draw)
    {
        if (!draw || !showTrajectory)
        {
            trajectoryLr.enabled = false; 
        }
    }
    public void Trajectory(Transform spawnPoint, PlayerColor playerActive, float projectileMass, float strength, bool draw = true)
    {
        if (!draw || !showTrajectory)
        {
            trajectoryLr.enabled = false;
            return;
        }
        trajectoryLr.colorGradient = colorsTrajectory[(int)playerActive];
        DrawProjection(spawnPoint, projectileMass, strength);
    }



    void DrawProjection(Transform spawnPoint, float projectileMass, float throwStrength)
    {
        trajectoryLr.enabled = true;
        trajectoryLr.positionCount = Mathf.CeilToInt(CONST_LinePoints / CONST_TimeBetweenPoints) + 1;
        Vector3 startPos = spawnPoint.position;
        Vector3 startVel = (throwStrength / projectileMass) * spawnPoint.forward;

        int i = 0;
        trajectoryLr.SetPosition(i, startPos);
        for (float time = 0; time < CONST_LinePoints; time += CONST_TimeBetweenPoints)
        {
            i++;
            Vector3 point = startPos + time * startVel;
            point.x = startPos.x + startVel.x * time + 0.5f * Physics.gravity.x * time * time;
            point.y = startPos.y + startVel.y * time + 0.5f * Physics.gravity.y * time * time;
            point.z = startPos.z + startVel.z * time + 0.5f * Physics.gravity.z * time * time;
            trajectoryLr.SetPosition(i, point);

            if (point.y < CONST_MinY) //this should replace floor collider
            {
                trajectoryLr.positionCount = i;
                return;
            }

            Vector3 lastPosition = trajectoryLr.GetPosition(i - 1);
            if (Physics.Raycast(lastPosition, (point - lastPosition).normalized, out RaycastHit hit, (point - lastPosition).magnitude, GameManager.Instance.layForTrajectory))
            {
                trajectoryLr.SetPosition(i, hit.point);
                trajectoryLr.positionCount = i + 1;
                return;
            }
        }
    }
}
public class LaunchVelocity 
{
    Vector3 _projectilePos, _targetPos, _gravity;
    float _maxHeight = 25f;
    float _moveX, _moveZ;

    public Vector3 Vel(Vector3 projectile, Vector3 tar, Vector3 gravityWind)
    {
        _projectilePos = projectile;
        _targetPos = tar;
        _gravity = gravityWind;

        if (_gravity.y < 0) _maxHeight = Mathf.Max(_projectilePos.y, _targetPos.y) + 0.5f;
        else _maxHeight = Mathf.Min(_projectilePos.y, _targetPos.y) - 0.5f;

        return CalculateLaunchData().initialVel;
    }


    LaunchData CalculateLaunchData()
    {
        float displacementY = (_targetPos.y +0.12f) - _projectilePos.y;
        float timeTotal = Mathf.Sqrt(-2 * _maxHeight / _gravity.y) + Mathf.Sqrt(2 * (displacementY - _maxHeight) / _gravity.y);

        Vector3 velY = Vector3.up * Mathf.Sqrt(- 2 * _gravity.y * _maxHeight);

        _moveX = 0.5f * _gravity.x * Mathf.Pow(timeTotal, 2);
        _moveZ = 0.5f * _gravity.z * Mathf.Pow(timeTotal, 2);
        Vector3 displacementXZ = new Vector3(_targetPos.x - _projectilePos.x - _moveX, 0f, _targetPos.z - _projectilePos.z - _moveZ);
        Vector3 velXZ = displacementXZ / timeTotal;


        return new LaunchData(velXZ + velY * (-Mathf.Sign(_gravity.y)), timeTotal);

    }


    struct LaunchData
    {
        public Vector3 initialVel;
        public float time;

        public LaunchData(Vector3 initialVel, float time)
        {
            this.initialVel = initialVel;
            this.time = time;
        }
    }

}






