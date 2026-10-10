using UnityEngine;
using UnityEngine.UI;

namespace SpeedTimeGraphLesson
{
    [ExecuteAlways, RequireComponent(typeof(CanvasRenderer))]
    public class SpeedTimeGraphPlot : MaskableGraphic
    {
        public SpeedTimeChallenge controller;
        public float maxTime = 10, maxSpeed = 8;
        public override Texture mainTexture => Texture2D.whiteTexture;
        public void RefreshMesh() { canvasRenderer.cull=false;UpdateGeometry();UpdateMaterial(); }
        public Vector2 Pixel(Vector2 p) => new Vector2(p.x / maxTime * rectTransform.rect.width, p.y / maxSpeed * rectTransform.rect.height) + rectTransform.rect.min;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (!controller) return;
            var points = controller.points;
            var r = rectTransform.rect;
            for (int i = 0; i <= 12; i++)
            {
                float x = r.xMin + r.width * i / 12;
                Line(vh, new Vector2(x, r.yMin), new Vector2(x, r.yMax), 1, new Color(.08f,.5f,.8f,.35f));
                float y = r.yMin + r.height * i / 12;
                Line(vh, new Vector2(r.xMin,y), new Vector2(r.xMax,y), 1, new Color(.08f,.5f,.8f,.35f));
            }
            if (controller.questionIndex > 0)
                for(int i=0;i<points.Length-1;i++)
                {
                    if(controller.mode!=0&&points[i].x>=controller.time)continue;
                    var a=Pixel(points[i]); var endpoint=points[i+1];
                    if(controller.mode!=0&&endpoint.x>controller.time)endpoint=new Vector2(controller.time,controller.Speed(controller.time));
                    var b=Pixel(endpoint);
                    Quad(vh,new Vector2(a.x,r.yMin),a,b,new Vector2(b.x,r.yMin),new Color(.1f,.8f,1,.16f));
                }
            Line(vh,new Vector2(r.xMin,r.yMax),r.min,3,new Color(.5f,.9f,1));
            Line(vh,r.min,new Vector2(r.xMax,r.yMin),3,new Color(.5f,.9f,1));
            for(int i=0;i<points.Length-1;i++)
            {
                Color c=points[i+1].y>points[i].y ? new Color(1,.55f,.08f) : points[i+1].y<points[i].y ? new Color(.35f,1,.3f) : new Color(1,.9f,.15f);
                Line(vh,Pixel(points[i]),Pixel(points[i+1]),12,new Color(c.r,c.g,c.b,.10f));
                Line(vh,Pixel(points[i]),Pixel(points[i+1]),4,c);
            }
            foreach(var p in points) { Circle(vh,Pixel(p),12,new Color(.2f,.85f,1,.2f)); Circle(vh,Pixel(p),8,Color.white); Circle(vh,Pixel(p),5,new Color(0,.8f,1)); }
            if(controller.mode!=0)
            {
                float x=Pixel(new Vector2(controller.time,0)).x;
                Line(vh,new Vector2(x,r.yMin),new Vector2(x,r.yMax),2,Color.white);
            }
        }
        static void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color)
        {
            int n=vh.currentVertCount; vh.AddVert(a,color,Vector2.zero); vh.AddVert(b,color,Vector2.zero); vh.AddVert(c,color,Vector2.zero); vh.AddVert(d,color,Vector2.zero); vh.AddTriangle(n,n+1,n+2); vh.AddTriangle(n,n+2,n+3);
        }
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color color)
        {
            Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f; Quad(vh,a-n,a+n,b+n,b-n,color);
        }
        static void Circle(VertexHelper vh,Vector2 p,float radius,Color color)
        {
            int start=vh.currentVertCount; vh.AddVert(p,color,Vector2.zero);
            for(int i=0;i<=24;i++) { float a=i*Mathf.PI*2/24; vh.AddVert(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,color,Vector2.zero); if(i>0) vh.AddTriangle(start,start+i,start+i+1); }
        }
    }
}
