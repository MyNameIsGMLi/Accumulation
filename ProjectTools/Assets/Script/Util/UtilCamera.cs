using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;

namespace ETHotfix
{
    //摄像机组件
    public class UtilCamera : MonoBehaviour
    {
        private UnityEngine.EventSystems.EventSystem eventSystem;


        private Camera mainCamera;


        public Camera MainCamera
        {
            get { return mainCamera; }
            set { mainCamera = value; }
        }

        private Plane perspectivePlan = new Plane(new Vector3(0, 1, 0), 0);//透视相机平面

        private float maxHorizontalDistance = 10000f;

        private Vector2 CamPosMin { get; set; }
        private Vector2 CamPosMax { get; set; }


        public Vector2 boundaryMin = new Vector2(-15.5F, 13);
        public Vector2 boundaryMax = new Vector2(150, 110);

        private Vector2 camOverdragMargin2d = Vector2.one * 5.0f;

        private bool is2dOverdragMarginEnabled = false;

        private float camOverdragMargin = 5.0f;


        private bool isDrag = false;
        private Vector3 dragCamPos = Vector3.zero;
        private Vector3 verticalCamPos = Vector3.zero;

        private Vector3 newHorzontalPos = Vector3.zero;
        private Vector3 newVerticalPos = Vector3.zero;

        private Vector3 horzontalMoveDir = Vector3.zero;
        private Vector3 verticalMoveDir = Vector3.zero;

        private Vector3 beforMovePos = Vector3.zero;

        private float beforMoveTime = 0;

        private float lockOffset_X = 5.7f;
        private float lockOffset_Y = 5.7f; 

        private List<LeanFinger> leanFingers = new List<LeanFinger>();
        private float touchDictance = 0;
        private float moveDistance = 0;

        private float verticalMinY = 9;
        private float verticalMaxY = 27;

        private float unLockOffsetScale = .75f;


        private float minOnlineScreenX = 0;
        private float maxOnlineScreenX = 0;
        private float minOnlineScreenY = 0;
        private float maxOnlineScreenY = 0;

        private int edageMoveBttom = 0;
        private int edageMoveLeft = 0;
        private float edageMoveSpeed = Time.unscaledDeltaTime*7.0f;
        
        /// <summary>
        /// 摄像机拖拽中
        /// </summary>
        public bool IsCameraDragging
        {
            get
            {
                if (isDrag)
                {
                    return horzontalMoveDir != Vector3.zero;
                }
                else
                {
                    return verticalMoveDir != Vector3.zero;
                }
            }
        }

        /// <summary>
        /// 摄像机可移动
        /// </summary>
        public bool IsCameraCanMove
        {
            get;set;
        }

        public virtual void InitTouch()
        {
            //Log.Error("Touch Init");
            eventSystem = GameObject.Find("EventSystem").GetComponent<UnityEngine.EventSystems.EventSystem>();

            LeanTouch.OnFingerDown += OnFingerDown;
            LeanTouch.OnFingerUpdate += OnFingerUpdate;
            LeanTouch.OnFingerUp += OnFingerUp;
            LeanTouch.OnFingerOld += OnFingerOld;
            LeanTouch.OnFingerTap += OnFingerTap;
            LeanTouch.OnFingerSwipe += OnFingerSwipe;
            LeanTouch.OnFingerExpired += OnFingerExpired;
            LeanTouch.OnFingerInactive += OnFingerInactive;
            LeanTouch.OnGesture += OnGesture;

            MainCamera = Camera.main;
            verticalCamPos = MainCamera.transform.position;
            IsCameraCanMove = true;
            RefreshBoundary();
            SetOnlineScreenPos();
        }


        /// <summary>
        ///  点击位置
        /// </summary>
        public Vector2 startPosition= Input.mousePosition;

        /// <summary>
        ///  上一次位置
        /// </summary>
        public Vector2 lastPosition = Input.mousePosition;

        /// <summary>
        ///  当前位置
        /// </summary>
        public Vector2 nowPosition = Input.mousePosition;
        
        /// <summary>
        ///  拖动比例
        /// </summary>
        public float ratio = 0.8f;

        private void OnFingerDown(LeanFinger obj)
        {
            StartDragCameraSetProp();
        }


        private void OnFingerUp(LeanFinger obj)
        {
            isDrag = false;
        }

        private void OnFingerUpdate(LeanFinger obj)
        {
            UpdateDragCameraPos(obj);
            AutoEdageCameraMove();
        }



        private void OnGesture(List<LeanFinger> obj)
        {
            
        }

        private void OnFingerInactive(LeanFinger obj)
        {
            
        }

        private void OnFingerExpired(LeanFinger obj)
        {
            
        }

        private void OnFingerSwipe(LeanFinger obj)
        {
           
        }

        private void OnFingerTap(LeanFinger obj)
        {
           
        }

        private void OnFingerOld(LeanFinger obj)
        {
            
        }
        
        
#region 相机基础功能

        private void StartDragCameraSetProp()
        {
            if (IsCameraCanMove)
            {
                lastPosition = Input.mousePosition;
                startPosition = Input.mousePosition;
                if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    leanFingers = LeanTouch.Fingers;
                    if (leanFingers.Count == 1)
                    {
                        lastPosition = leanFingers[0].ScreenPosition;
                        startPosition = leanFingers[0].ScreenPosition;
                        isDrag = true;
                    }
                    else if (leanFingers.Count == 2)
                    {
                        isDrag = false;
                        touchDictance = Vector3.Distance(leanFingers[0].ScreenPosition, leanFingers[1].ScreenPosition);
                    }
                } else if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor)
                {
                    isDrag = true;
                }
                nowPosition = Vector2.zero;
                dragCamPos = MainCamera.transform.position;
                verticalCamPos = MainCamera.transform.position;
            }
        }
        private void UpdateDragCameraPos(LeanFinger obj)
        {
            if (IsCameraCanMove)
            {
                if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    leanFingers = LeanTouch.Fingers;
                    DragCamera(isDrag && leanFingers.Count == 1, obj);
                }
                else if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor)
                {
                    DragCamera(isDrag,obj);
                }
            }
        }
        private void DragCamera(bool isDrag,LeanFinger obj)
        {
            if (isDrag)
            {
                nowPosition = obj.ScreenPosition;
                newHorzontalPos = GetHorzontalMove();
                UpdateCameraPosition(newHorzontalPos);
                lastPosition = nowPosition;
            }
            else
            {
                newVerticalPos = GetVerticalMove();
                UpdateCameraPosition(newVerticalPos);
            }
        }

        private void AutoEdageCameraMove()
        {
            if (CityLayoutComponent.Instance.GetCurrentDragginItem == null)
                return;
            
            if (Input.mousePosition.x <= minOnlineScreenX)
            {
                edageMoveLeft = 1;
            }
            else if (Input.mousePosition.x >= maxOnlineScreenX)
            {
                edageMoveLeft = -1;
            }
            else
            {
                edageMoveLeft = 0;
            }
            
            if (Input.mousePosition.y <= minOnlineScreenY)
            {
                edageMoveBttom = 1;
            }
            else if (Input.mousePosition.y >= maxOnlineScreenY)
            {
                edageMoveBttom = -1;
            }
            else
            {
                edageMoveBttom = 0;
            }
            Vector3 startPos = mainCamera.transform.position;
            Vector3 nextCameraPos = new Vector3(startPos.x + edageMoveLeft * edageMoveSpeed, startPos.y + edageMoveBttom * edageMoveSpeed,
                startPos.z);
            Vector3 newCameraPos = startPos - GetMoveVector(startPos, nextCameraPos) * 40.0f;
            Vector3 clampXZPos = GetClampToBoundaries(newCameraPos);
            Vector3 clampYPos = GetClampToVertical(clampXZPos);
            
            UpdateCameraPosition(clampYPos);
        }
        
        /// <summary>
        /// 更新摄像机位置
        /// </summary>
        /// <param name="targetPos"></param>
        private void UpdateCameraPosition(Vector3 targetPos)
        {
            MainCamera.transform.position = targetPos;
            verticalCamPos = targetPos;
        }


        /// <summary>
        /// 更新摄像机位置（动画移动）
        /// </summary>
        /// <param name="targetPos"></param>
        /// <param name="time"></param>
        private void UpdateCameraPosTween(Vector3 targetPos, float duration, Action callback = null,bool isSaveBeforPos = true)
        {
            bool isSmall = Vector3.Distance(MainCamera.transform.position, targetPos) < 1;

            duration = isSmall ? .03f : duration;

            Define.IsCameraMove = true;

            if (isSaveBeforPos)
            {
                beforMovePos = MainCamera.transform.position;
                beforMoveTime = duration;
            }

            MainCamera.transform.DOMove(targetPos, duration).OnComplete(() =>
            {
                UpdateCameraPosition(targetPos);
                IsCameraCanMove = true;
                Define.IsCameraMove = false;
                callback?.Invoke();
            });
        }

        /// <summary>
        /// 回退摄像机位置
        /// </summary>
        public void GoBackCameraPos()
        {
           // UpdateCameraPosTween(beforMovePos, .12f, null, false);
        } 

        /// <summary>
        /// 获取水平移动值
        /// </summary>
        /// <returns></returns>
        private Vector3 GetHorzontalMove()
        {
            horzontalMoveDir = GetMoveVector(startPosition, lastPosition);
            return GetClampToBoundaries(dragCamPos - horzontalMoveDir);
        }

        /// <summary>
        /// 获取垂直移动值
        /// </summary>
        /// <returns></returns>
        private Vector3 GetVerticalMove()
        {

            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                if (leanFingers.Count != 2)
                {
                    return verticalCamPos;
                }

                moveDistance = Vector3.Distance(leanFingers[0].ScreenPosition, leanFingers[1].ScreenPosition);
                // float dicAbs = Mathf.Abs(moveDistance - touchDictance);
                // Log.Debug("缩放差值：" + dicAbs);
                if (Mathf.Abs(moveDistance- touchDictance)<10)
                {
                    return verticalCamPos;
                }

                if (touchDictance < moveDistance)
                {
                    touchDictance = moveDistance;
                    moveDistance = .1f;
                }
                else if (touchDictance > moveDistance)
                {
                    touchDictance = moveDistance;
                    moveDistance = -.1f;
                }
                else
                {
                    moveDistance = 0;
                }

            }
            else if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor)
            {
                moveDistance = Input.GetAxis("Mouse ScrollWheel");
            }

            if (moveDistance == 0)
            {
                return verticalCamPos;
            }

            Vector3 centerIntersectionPoint = GetScreenCenterIntersectionPos();

            Vector3 offset = Vector3.Lerp(Vector3.zero, (verticalCamPos - centerIntersectionPoint), moveDistance * Time.unscaledDeltaTime );
            verticalMoveDir = (verticalCamPos - (centerIntersectionPoint + offset)).normalized * .5f;


            if (moveDistance > 0 && MainCamera.transform.position.y >verticalMinY)
            {
                return GetClampToVertical(verticalCamPos - verticalMoveDir);
            }
            else if (moveDistance < 0 && MainCamera.transform.position.y < verticalMaxY)
            {
                return GetClampToVertical(verticalCamPos + verticalMoveDir);
            }
            else
            {
                return verticalCamPos;
            }
        }


        /// <summary>
        /// 获取锁定移动值
        /// </summary>
        /// <param name="targetPos"></param>
        public void GetLockTargetMove(Vector3 targetPos,Vector2 mousePosition, Action callback = null)
        {
            Vector3 targetIntersectionPoint = targetPos;
            Vector3 camIntersectionPoint = GetScreenCenterIntersectionPos();
            Vector3 offset = Vector3.Lerp(Vector3.zero, (camIntersectionPoint - targetIntersectionPoint), Time.unscaledTime);
            Vector3 moveDir = camIntersectionPoint - (targetIntersectionPoint);
            Vector3 newPos = GetClampToBoundaries(camIntersectionPoint - moveDir);

            newPos = GetClampToVertical(newPos);
            newPos = new Vector3(newPos.x - lockOffset_X, newPos.y, newPos.z + lockOffset_Y);


            float duration = Vector3.Distance(MainCamera.transform.position, newPos) / Time.unscaledTime * .5f;

            UpdateCameraPosTween(newPos, duration, callback);
        }


        public void GetUnLockTargetMove(Vector3 targetPos, Action callback = null)
        {
            Vector3 startPos = GetIntersectionPoint(new Ray(MainCamera.transform.position, -perspectivePlan.normal));
            Vector3 endPos = GetIntersectionPoint(new Ray(targetPos, -perspectivePlan.normal));
            Vector3 dir = startPos - endPos;
            Vector3 newPos = GetClampToBoundaries(mainCamera.transform.position - dir);

            int count = (int)(newPos.y - 9f);
            if (count <= 0) 
                count = 0;
            
            newPos = new Vector3(newPos.x - lockOffset_X - count * unLockOffsetScale, newPos.y, newPos.z + lockOffset_Y + count * unLockOffsetScale);
            float duration = Vector3.Distance(MainCamera.transform.position, newPos) / Time.unscaledTime * .5f;
            UpdateCameraPosTween(newPos, duration, callback);
        }


        /// <summary>
        /// 刷新边界
        /// </summary>
        protected void RefreshBoundary()
        {
            Vector2 camProjectedMin = Vector2.zero;
            Vector2 camProjectedMax = Vector2.zero;

            Vector2 camProjectedCenter = GetIntersection2d(new Ray(MainCamera.transform.position, -perspectivePlan.normal));
     
            Vector2 camRight = GetIntersection2d(MainCamera.ScreenPointToRay(new Vector3(Screen.width, Screen.height * 0.5f, 0)));
            Vector2 camLeft = GetIntersection2d(MainCamera.ScreenPointToRay(new Vector3(0, Screen.height * 0.5f, 0)));
            Vector2 camUp = GetIntersection2d(MainCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height, 0)));
            Vector2 camDown = GetIntersection2d(MainCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, 0, 0)));

            camProjectedMin = GetVector2Min(camRight, camLeft, camUp, camDown);
            camProjectedMax = GetVector2Max(camRight, camLeft, camUp, camDown);

            Vector2 projectionCorrectionMin = camProjectedCenter - camProjectedMin;
            Vector2 projectionCorrectionMax = camProjectedCenter - camProjectedMax;

            CamPosMin = boundaryMin + projectionCorrectionMin;
            CamPosMax = boundaryMax + projectionCorrectionMax;

            Vector2 margin = CamOverdragMargin2d;
            if (CamPosMax.x - CamPosMin.x < margin.x * 2)
            {
                float midPoint = (CamPosMax.x + CamPosMin.x) * 0.5f;
                CamPosMax = new Vector2(midPoint + margin.x, CamPosMax.y);
                CamPosMin = new Vector2(midPoint - margin.x, CamPosMin.y);
            }

            if (CamPosMax.y - CamPosMin.y < margin.y * 2)
            {
                float midPoint = (CamPosMax.y + CamPosMin.y) * 0.5f;
                CamPosMax = new Vector2(CamPosMax.x, midPoint + margin.y);
                CamPosMin = new Vector2(CamPosMin.x, midPoint - margin.y);
            }
            Log.Debug("摄像机边界刷新成功");
        }


        private void SetOnlineScreenPos()
        {
            minOnlineScreenX = Screen.width * 0.05f;
            maxOnlineScreenX = Screen.width - Screen.width * 0.05f;
            minOnlineScreenY = Screen.height * 0.05f;
            maxOnlineScreenY = Screen.height - Screen.height * 0.05f;
        }
        
        public Vector2 CamOverdragMargin2d
        {
            get
            {
                if (is2dOverdragMarginEnabled)
                {
                    return camOverdragMargin2d;
                }
                else
                {
                    return Vector2.one * camOverdragMargin;
                }
            }
            set
            {
                camOverdragMargin2d = value;
                camOverdragMargin = value.x;
            }
        }

        /// <summary>
        /// 获取屏幕中心射线交点
        /// </summary>
        /// <returns></returns>
        private Vector3 GetScreenCenterIntersectionPos()
        {
            Vector2 centerPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
            return GetIntersectionPoint(MainCamera.ScreenPointToRay(centerPoint));
        }


        /// <summary>
        /// 获取屏幕射线与给定平面的交点
        /// </summary>
        /// <param name="ray"></param>
        /// <returns></returns>
        private Vector3 GetIntersectionPoint(Ray ray)
        {
            Vector3 intersectionPoint = Vector3.zero;
            float distance = 0;
            bool isIntersection = perspectivePlan.Raycast(ray, out distance);


            if (isIntersection && distance < maxHorizontalDistance)//相交
            {
                intersectionPoint = ray.origin + ray.direction * distance;
            }
            else
            {
                intersectionPoint = UnProjectVector2(ray.origin) + UnProjectVector2(ray.direction) * maxHorizontalDistance;
            }
            return intersectionPoint; 
        }

        public Vector3 GetClampToBoundaries(Vector3 newPosition, bool includeSpringBackMargin = false)
        {
            Vector2 margin = Vector2.zero;
            if (includeSpringBackMargin == true)
            {
                margin = CamOverdragMargin2d;
            }
            newPosition.x = Mathf.Clamp(newPosition.x, CamPosMin.x + margin.x, CamPosMax.x - margin.x);
            newPosition.z = Mathf.Clamp(newPosition.z, CamPosMin.y + margin.y, CamPosMax.y - margin.y);
            float y = Mathf.Clamp(newPosition.y, verticalMinY, verticalMaxY);
            Vector3 v3 = new Vector3(newPosition.x, y, newPosition.z);
            return (v3);
        }

        public Vector3 GetClampToVertical(Vector3 newPosition)
        {
            float y = Mathf.Clamp(newPosition.y, verticalMinY, verticalMaxY);
            Vector3 v3 = new Vector3(newPosition.x, y, newPosition.z);
            return GetClampToBoundaries(v3);
        }

        private Vector2 GetIntersection2d(Ray ray)
        {
            Vector3 intersection3d = GetIntersectionPoint(ray);
            Vector2 intersection2d = new Vector2(intersection3d.x, 0);
            intersection2d.y = intersection3d.z;
            return (intersection2d);
        }

        private Vector3 UnProjectVector2(Vector2 v2, float offer = 0)
        {
            return new Vector3(v2.x, 0, v2.y);
        }
        private Vector2 GetVector2Min(Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3)
        {
            return new Vector2(Mathf.Min(v0.x, v1.x, v2.x, v3.x), Mathf.Min(v0.y, v1.y, v2.y, v3.y));
        }

        private Vector2 GetVector2Max(Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3)
        {
            return new Vector2(Mathf.Max(v0.x, v1.x, v2.x, v3.x), Mathf.Max(v0.y, v1.y, v2.y, v3.y));
        }


        private void CreatPoint(Vector3 pos, string name, Color color)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.GetComponent<Renderer>().material.color = color;
            obj.transform.position = pos;
            obj.transform.name = name;
        }

        /// <summary>
        /// 移动朝向
        /// </summary>
        /// <param name="startPos"></param>
        /// <param name="dragPosCurrent"></param>
        /// <returns></returns>
        private Vector3 GetMoveVector(Vector3 startPos, Vector3 dragPosCurrent)
        {
            Vector3 dragStart = GetIntersectionPoint(MainCamera.ScreenPointToRay(startPos));
            Vector3 dragCurrent = GetIntersectionPoint(MainCamera.ScreenPointToRay(dragPosCurrent));
            return (dragCurrent - dragStart);
        }
        
        public void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector2 boundaryCenter2d = 0.5f * (boundaryMin + boundaryMax);
            Vector2 boundarySize2d = boundaryMax - boundaryMin;
            Vector3 boundaryCenter = UnProjectVector2(boundaryCenter2d, 0);
            Vector3 boundarySize = UnProjectVector2(boundarySize2d);
            Gizmos.DrawWireCube(boundaryCenter, boundarySize);
        }

#endregion
    }
}
