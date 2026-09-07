using System.Collections.Generic;
using UnityEngine;

namespace GothicTactics.Skirmish
{
    // Original, code-drawn pixel assets. No external art packs or import setup required.
    public sealed class AshenPixelArt
    {
        private readonly List<Object> owned = new List<Object>();
        private readonly Dictionary<string,Sprite> characters = new Dictionary<string,Sprite>();
        private Sprite flame;
        public readonly Texture2D Stone, Panel, Button;
        private static Color32 C(string hex) { ColorUtility.TryParseHtmlString("#"+hex,out Color c); return c; }
        public AshenPixelArt()
        {
            Stone = Masonry(64,64,71,false);
            Panel = Masonry(64,64,98,true);
            Button = Masonry(32,32,52,true);
        }
        private Texture2D Texture(int w,int h,Color32[] pixels,string name)
        {
            var t = new Texture2D(w,h,TextureFormat.RGBA32,false) { name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            t.SetPixels32(pixels); t.Apply(); owned.Add(t); return t;
        }
        private Texture2D Masonry(int w,int h,int seed,bool dark)
        {
            var random = new System.Random(seed); var pixels = new Color32[w*h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                int row = y/16, bx = (x+(row%2)*16)%32, by = y%16;
                int noise = random.Next(-11,12), v = (dark ? 34 : 145)+noise+(row%3)*4;
                if (bx < 2 || by < 2) v = dark ? 18 : 45;
                else if (by == 2 || bx == 2) v += dark ? 8 : 27;
                else if (by == 15 || bx == 31) v -= 20;
                if (!dark && ((x*13+y*7)%113 < 3)) v -= 34;
                byte r = (byte)Mathf.Clamp(v,0,255), g = (byte)Mathf.Clamp(v*.91f,0,255), b = (byte)Mathf.Clamp(v*.77f,0,255);
                pixels[y*w+x] = new Color32(r,g,b,255);
            }
            return Texture(w,h,pixels,dark ? "Ashen iron grain" : "Cracked crypt flagstones");
        }
        private sealed class Canvas
        {
            public readonly int W,H; public readonly Color32[] Pixels;
            public Canvas(int w,int h) { W=w; H=h; Pixels=new Color32[w*h]; }
            public void Dot(int x,int y,Color32 c) { if(x>=0 && x<W && y>=0 && y<H) Pixels[(H-1-y)*W+x]=c; }
            public void Box(int x,int y,int w,int h,string hex)
            { for(int yy=y;yy<y+h;yy++) for(int xx=x;xx<x+w;xx++) Dot(xx,yy,C(hex)); }
            public void Shape(string hex,params int[] points)
            {
                for(int y=0;y<H;y++) for(int x=0;x<W;x++)
                {
                    bool inside=false; int count=points.Length/2;
                    for(int i=0,j=count-1;i<count;j=i++)
                    {
                        int xi=points[i*2],yi=points[i*2+1],xj=points[j*2],yj=points[j*2+1];
                        if ((yi>y)!=(yj>y) && x < (float)(xj-xi)*(y-yi)/(yj-yi)+xi) inside=!inside;
                    }
                    if(inside) Dot(x,y,C(hex));
                }
            }
        }
        private Sprite Finish(Canvas c,string name,float ppu)
        {
            var texture = Texture(c.W,c.H,c.Pixels,name);
            texture.wrapMode = TextureWrapMode.Clamp;
            var sprite = Sprite.Create(texture,new Rect(0,0,c.W,c.H),new Vector2(.5f,0),ppu,0,SpriteMeshType.FullRect);
            sprite.name=name; owned.Add(sprite); return sprite;
        }
        public Sprite Character(string role)
        {
            if(characters.TryGetValue(role,out var cached)) return cached;
            var p = new Canvas(40,56);
            bool warden=role=="Warden", knight=role=="Knight", ranger=role=="Arbalist", witch=role=="Hexblade", invoker=role=="Invoker";
            if(role=="Hound")
            {
                p.Shape("171415",3,40,8,30,19,28,30,33,35,40,30,49,6,49);
                p.Shape("65514a",7,35,18,31,29,35,26,43,9,43);
                p.Shape("997363",8,34,17,32,25,35,16,37);
                p.Shape("382a29",24,34,28,27,32,34,37,39,32,44,25,41);
                p.Box(29,35,2,2,"e85d2b"); p.Box(33,40,3,2,"c3ac87");
                p.Box(8,42,4,10,"362c2b");p.Box(23,42,4,11,"362c2b");
                p.Box(7,51,6,2,"857362");p.Box(22,52,7,2,"857362");
                var hound = Finish(p,"Crypt hound",23); characters[role]=hound; return hound;
            }
            string cloak = invoker ? "512a43" : witch ? "63302e" : ranger ? "424638" : knight ? "3b272b" : "343b40";
            string mid = warden ? "777d78" : knight ? "655754" : ranger ? "776b4e" : "82695a";
            p.Shape("131315",12,16,24,15,29,25,30,42,24,51,9,49,8,29);
            p.Shape(cloak,12,19,24,19,27,32,30,47,23,49,19,39,11,49,7,46);
            p.Shape(mid,13,21,22,20,25,27,22,35,13,35,10,27);
            p.Shape("b2aa8b",13,22,17,22,15,31,12,29);
            p.Shape("3d3632",21,23,24,25,22,35,18,34);
            p.Box(12,34,12,3,"302621");p.Box(18,34,3,2,"c5a05c");
            p.Shape("292728",12,37,18,38,16,51,11,52);
            p.Shape("4a4034",19,38,24,37,25,52,20,52);
            p.Box(9,51,8,3,"80715a");p.Box(20,52,8,2,"80715a");
            p.Shape("17171b",12,9,17,5,23,7,25,16,22,22,14,21,11,16);
            if(warden || knight)
            {
                p.Shape(mid,14,10,18,7,22,9,23,18,18,20,13,17);
                p.Box(15,10,3,6,"acaa96");p.Box(14,16,9,2,"211d20");p.Box(19,17,2,4,"aaa186");
                if(knight) { p.Box(14,16,2,1,"ea7d42");p.Box(20,16,2,1,"ea7d42"); }
                p.Shape("211c1b",2,26,9,23,14,28,12,40,7,45,2,40);
                p.Shape(warden ? "655c43" : "572e30",4,27,9,25,12,29,10,39,7,42,4,39);
                p.Box(7,28,2,12,"b2a17a");p.Box(4,32,7,2,"b2a17a");
                p.Shape("222022",29,6,33,3,34,32,30,32);
                p.Shape("aaa99b",31,8,33,5,32,30,30,30);p.Box(31,11,1,17,"e1d9b3");
                p.Box(27,30,10,2,"ab874b");p.Box(31,32,2,9,"66513c");p.Box(27,31,4,4,"ad9270");
            }
            else
            {
                p.Shape(cloak,13,10,18,6,22,8,24,18,20,14,14,18);
                p.Box(15,15,7,5,invoker ? "b2b09a" : "b49b7c");
                p.Box(16,16,2,1,"282125");p.Box(21,16,1,1,"282125");p.Box(17,20,4,2,"584137");
                if(ranger)
                {
                    p.Shape("2a211c",24,27,35,20,38,24,28,35,24,35);
                    p.Shape("a18b60",26,29,35,23,36,25,28,33);
                    p.Shape("9b927c",24,21,27,22,36,33,34,35);
                    p.Box(26,29,4,3,"b79a75");
                }
                else
                {
                    p.Box(30,12,3,40,"30201b");p.Box(30,14,1,36,"a48a54");
                    p.Shape("88613c",26,9,31,4,36,9,33,16,28,15);
                    p.Shape(invoker ? "ac5350" : "9c9573",28,9,31,6,34,10,31,14);
                    p.Box(30,8,2,3,"eee0ab");p.Box(27,29,4,4,"b09476");
                }
                if(role=="Thrall")
                {
                    p.Box(14,11,9,9,"a29d81");p.Box(15,14,3,2,"261d1b");p.Box(20,14,2,2,"261d1b");
                    p.Box(17,19,5,3,"c0b599");p.Box(19,20,1,2,"30241e");
                }
            }
            var result = Finish(p,role+" pixel character",23); characters[role]=result; return result;
        }
        public Sprite Flame()
        {
            if(flame!=null) return flame;
            var p = new Canvas(12,24);
            p.Shape("592619",2,17,5,9,5,2,9,10,10,19,6,23);
            p.Shape("cf642a",3,17,6,6,8,13,9,19,6,22);
            p.Shape("f4b44d",4,17,6,11,8,19,6,22);
            p.Box(5,17,2,4,"fff0ad"); flame = Finish(p,"Torch flame",24); return flame;
        }
        public void Dispose() { foreach(var asset in owned) if(asset!=null) Object.Destroy(asset); owned.Clear(); }
    }
}
