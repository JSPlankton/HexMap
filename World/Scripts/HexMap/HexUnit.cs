using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace JS.HexMap
{
    public class HexUnit : MonoBehaviour
    {
        public static HexUnit unitPrefab;
        public HexGrid Grid { get; set; }
        List<HexCell> pathToTravel;
        const float travelSpeed = 2f;
        const float rotationSpeed = 180f;
        const int visionRange = 3;
        public HexCell Location {
            get {
                return location;
            }
            set {
                if (location) {
                    Grid.DecreaseVisibility(location, visionRange);
                    location.Unit = null;
                }
                location = value;
                value.Unit = this;
                Grid.IncreaseVisibility(value, visionRange);
                transform.localPosition = value.Position;
            }
        }

        HexCell location, currentTravelLocation;
        
        public float Orientation {
            get {
                return orientation;
            }
            set {
                orientation = value;
                transform.localRotation = Quaternion.Euler(0f, value, 0f);
            }
        }

        float orientation;

        public void ValidateLocation () {
            transform.localPosition = location.Position;
        }
        
        public bool IsValidDestination (HexCell cell) {
            return !cell.IsUnderwater && !cell.Unit;;
        }
        
        public void Die () {
            if (location) {
                Grid.DecreaseVisibility(location, visionRange);
            }
            location.Unit = null;
            Destroy(gameObject);
        }
        
        public void Travel (List<HexCell> path) {
            location.Unit = null;
            location = path[path.Count - 1];
            location.Unit = this;
            pathToTravel = path;
            StopAllCoroutines();
            StartCoroutine(TravelPath());
        }
        
        public void Save (BinaryWriter writer) {
            location.coordinates.Save(writer);
            writer.Write(orientation);
        }
        
        public static void Load (BinaryReader reader, HexGrid grid) {
            HexCoordinates coordinates = HexCoordinates.Load(reader);
            float orientation = reader.ReadSingle();
            grid.AddUnit(
                Instantiate(unitPrefab), grid.GetCell(coordinates), orientation
            );
        }
        
        IEnumerator TravelPath () {
            Vector3 a, b, c = pathToTravel[0].Position;
            yield return LookAt(pathToTravel[1].Position);
            Grid.DecreaseVisibility(pathToTravel[0], visionRange);
            float t = Time.deltaTime * travelSpeed;
            for (int i = 1; i < pathToTravel.Count; i++) {
                currentTravelLocation = pathToTravel[i];
                a = c;
                b = pathToTravel[i - 1].Position;
                c = (b + currentTravelLocation.Position) * 0.5f;
                Grid.IncreaseVisibility(pathToTravel[i], visionRange);
                for (; t < 1f; t += Time.deltaTime * travelSpeed) {
                    transform.localPosition = BezierUtil.GetPoint(a, b, c, t);
                    Vector3 d = BezierUtil.GetDerivative(a, b, c, t);
                    d.y = 0f;
                    transform.localRotation = Quaternion.LookRotation(d);
                    yield return null;
                }
                Grid.DecreaseVisibility(currentTravelLocation ? currentTravelLocation : pathToTravel[0], visionRange);
                t -= 1f;
            }
            currentTravelLocation = null;

            a = c;
            b = location.Position; // We can simply use the destination here.
            c = b;
            Grid.IncreaseVisibility(location, visionRange);
            for (; t < 1f; t += Time.deltaTime * travelSpeed) {
                transform.localPosition = BezierUtil.GetPoint(a, b, c, t);
                Vector3 d = BezierUtil.GetDerivative(a, b, c, t);
                d.y = 0f;
                transform.localRotation = Quaternion.LookRotation(d);
                yield return null;
            }
            
            transform.localPosition = location.Position;
            orientation = transform.localRotation.eulerAngles.y;
            
            ListPool<HexCell>.Add(pathToTravel);
            pathToTravel = null;
        }
        
        IEnumerator LookAt (Vector3 point) {
            point.y = transform.localPosition.y;
            Quaternion fromRotation = transform.localRotation;
            Quaternion toRotation =
                Quaternion.LookRotation(point - transform.localPosition);
            float angle = Quaternion.Angle(fromRotation, toRotation);
            if (angle > 0f)
            {
                float speed = rotationSpeed / angle;

                for (float t = Time.deltaTime * speed; t < 1f; t += Time.deltaTime * speed)
                {
                    transform.localRotation =
                        Quaternion.Slerp(fromRotation, toRotation, t);
                    yield return null;
                }

                transform.LookAt(point);
                orientation = transform.localRotation.eulerAngles.y;
            }
        }

        #region LifeCircle

        void OnEnable () {
            if (location) {
                transform.localPosition = location.Position;
                if (currentTravelLocation) {
                    Grid.IncreaseVisibility(location, visionRange);
                    Grid.DecreaseVisibility(currentTravelLocation, visionRange);
                    currentTravelLocation = null;
                }
            }
        }

        #endregion

        #region Editor
        
        #endregion
    }
}


