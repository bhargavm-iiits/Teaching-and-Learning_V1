using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace SpeedTimeGraphLesson.VR
{
    [ExecuteAlways,RequireComponent(typeof(CanvasRenderer))]
    public class VRGraphPlot : MaskableGraphic,IPointerDownHandler,IPointerUpHandler,IDragHandler,IInitializePotentialDragHandler,IEndDragHandler
    {
        public VRMotionChallenge controller;
        public TMP_Text timeAxis;
        public float maxTime=6,maxSpeed=60;
        int selected=-1,pointer;
        public override Texture mainTexture=>Texture2D.whiteTexture;
        public Vector2 Pixel(Vector2 p)=>rectTransform.rect.min+new Vector2(p.x/maxTime*rectTransform.rect.width,p.y/maxSpeed*rectTransform.rect.height);
        public Vector2 DisplayPoint(int i,int lane)=>Pixel(new Vector2(controller.profile[i].x,controller.profile[i].y*controller.startingSpeeds[lane]));
        public void RefreshMesh() { canvasRenderer.cull=false;UpdateGeometry();UpdateMaterial(); }
        public void OnInitializePotentialDrag(PointerEventData e) { e.useDragThreshold=false; }
        public void OnPointerDown(PointerEventData e)
        {
            if(!controller.CanEdit||selected>=0||!LocalPoint(e,out var local))return;
            float nearest=28;
            for(int i=1;i<controller.profile.Length;i++) { float d=Vector2.Distance(local,DisplayPoint(i,controller.selectedVehicle));if(d<nearest) { selected=i;nearest=d; } }
            if(selected>=0) { pointer=e.pointerId;VRMotionChallenge.Haptic(.15f); }
        }
        public void OnDrag(PointerEventData e) { if(selected>=0&&e.pointerId==pointer&&LocalPoint(e,out var local))controller.EditPoint(selected,local); }
        public void OnPointerUp(PointerEventData e) { if(e.pointerId==pointer) { selected=-1;controller.UpdateLabels(); } }
        public void OnEndDrag(PointerEventData e)=>OnPointerUp(e);
        bool LocalPoint(PointerEventData e,out Vector2 local)
        {
            var plane=new Plane(rectTransform.forward,rectTransform.position);local=default;
            if(e is TrackedDeviceEventData tracked&&tracked.rayPoints!=null)
            {
                for(int i=1;i<tracked.rayPoints.Count;i++)
                {
                    var a=tracked.rayPoints[i-1];var b=tracked.rayPoints[i];var ray=new Ray(a,b-a);
                    if(plane.Raycast(ray,out float d)&&d<=(b-a).magnitude+.001f) { local=rectTransform.InverseTransformPoint(ray.GetPoint(d));return true; }
                }
                return false;
            }
            var eye=e.pressEventCamera?e.pressEventCamera:e.enterEventCamera?e.enterEventCamera:controller.eye;
            if(!eye)return false;var desktopRay=eye.ScreenPointToRay(e.position);
            if(!plane.Raycast(desktopRay,out float distance))return false;local=rectTransform.InverseTransformPoint(desktopRay.GetPoint(distance));return true;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(!controller||controller.profile==null)return;var r=rectTransform.rect;
            if(timeAxis)timeAxis.text=$"Time (s)  |  0 - {maxTime:0}";
            for(int i=0;i<=12;i++) { float x=r.xMin+r.width*i/12,y=r.yMin+r.height*i/12;Line(vh,new Vector2(x,r.yMin),new Vector2(x,r.yMax),1,new Color(.1f,.5f,.8f,.25f));Line(vh,new Vector2(r.xMin,y),new Vector2(r.xMax,y),1,new Color(.1f,.5f,.8f,.25f)); }
            if(controller.questionIndex>0)
                for(int i=0;i<controller.profile.Length-1;i++)
                {
                    var a=controller.profile[i];var b=controller.profile[i+1];if(controller.mode!=0&&a.x>=controller.time)continue;
                    if(controller.mode!=0&&b.x>controller.time)b=new Vector2(controller.time,controller.NormalizedSpeed(controller.time));
                    a.y*=controller.SpeedScale;b.y*=controller.SpeedScale;var pa=Pixel(a);var pb=Pixel(b);Quad(vh,new Vector2(pa.x,r.yMin),pa,pb,new Vector2(pb.x,r.yMin),new Color(.1f,.8f,1,.13f));
                }
            var colors=new[]{new Color(.15f,.8f,1),new Color(1,.25f,.35f),new Color(1,.63f,.1f)};
            for(int lane=0;lane<3;lane++)
            {
                if(lane==controller.selectedVehicle)continue;
                var c=colors[lane];c.a=.38f;
                for(int i=0;i<controller.profile.Length-1;i++)Line(vh,DisplayPoint(i,lane),DisplayPoint(i+1,lane),2,c);
            }
            for(int i=0;i<controller.profile.Length-1;i++)
            {
                float dy=controller.profile[i+1].y-controller.profile[i].y;var c=dy>.001f?new Color(1,.58f,.08f):dy<-.001f?new Color(.3f,1,.45f):new Color(1,.88f,.1f);
                Line(vh,DisplayPoint(i,controller.selectedVehicle),DisplayPoint(i+1,controller.selectedVehicle),5,c);
            }
            for(int i=0;i<controller.profile.Length;i++) { var p=DisplayPoint(i,controller.selectedVehicle);Circle(vh,p,i==0?9:12,Color.white);Circle(vh,p,i==0?6:8,colors[controller.selectedVehicle]); }
            Line(vh,r.min,new Vector2(r.xMin,r.yMax),2,Color.white);Line(vh,r.min,new Vector2(r.xMax,r.yMin),2,Color.white);
            if(controller.mode!=0) { float x=Pixel(new Vector2(controller.time,0)).x;Line(vh,new Vector2(x,r.yMin),new Vector2(x,r.yMax),2,Color.white); }
        }
        static void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color)
        {
            int n=vh.currentVertCount;vh.AddVert(a,color,new Vector2(.5f,.5f));vh.AddVert(b,color,new Vector2(.5f,.5f));vh.AddVert(c,color,new Vector2(.5f,.5f));vh.AddVert(d,color,new Vector2(.5f,.5f));vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color c) { var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;Quad(vh,a-n,a+n,b+n,b-n,c); }
        static void Circle(VertexHelper vh,Vector2 p,float radius,Color c)
        {
            int n=vh.currentVertCount;vh.AddVert(p,c,Vector2.zero);for(int i=0;i<=24;i++) { float a=i*Mathf.PI*2/24;vh.AddVert(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,c,Vector2.zero);if(i>0)vh.AddTriangle(n,n+i,n+i+1); }
        }
    }
}
