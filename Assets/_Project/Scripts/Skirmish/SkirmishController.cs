using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GothicTactics.Skirmish
{
    public sealed partial class SkirmishController : MonoBehaviour
    {
        private SkirmishBattle battle;
        private SkirmishBattle.Unit selected;
        private Camera view;
        private Camera presentation;
        private Transform world;
        private readonly Dictionary<int, Renderer> tiles = new Dictionary<int, Renderer>();
        private readonly Dictionary<SkirmishBattle.Unit, Transform> pieces = new Dictionary<SkirmishBattle.Unit, Transform>();
        private readonly List<Material> materials = new List<Material>();
        private readonly Dictionary<int, int> costs = new Dictionary<int, int>();
        private readonly Color stone = new Color(.62f,.58f,.50f), teal = new Color(.53f,.57f,.38f), red = new Color(.65f,.22f,.16f);
        private AshenPixelArt art;
        private RenderTexture pixelScene;
        private Rect sceneRect; // Screen pixels, bottom-left origin.
        private readonly List<Light> torches = new List<Light>();
        private readonly List<SpriteRenderer> actors = new List<SpriteRenderer>();
        private Mesh hex;
        private int hover = -1;
        private bool busy;
        private string hint = "Select a hunter, then choose a lit hex or enemy.";
        private GUIStyle title, body, small, button;
        private float UiScale => Mathf.Max(.4f, Mathf.Min(Screen.width/1280f, Screen.height/800f));
        private Vector3 focus = new Vector3(12f,0,6f);
        private void Start()
        {
            view = Camera.main;
            if (view == null)
            {
                var go = new GameObject("Skirmish Camera", typeof(Camera), typeof(AudioListener));
                go.tag = "MainCamera"; view = go.GetComponent<Camera>();
            }
            art = new AshenPixelArt();
            // Keep a display camera active while the world camera renders offscreen.
            presentation = new GameObject("Pixel View Presentation",typeof(Camera)).GetComponent<Camera>();
            presentation.transform.SetParent(transform,false);
            presentation.cullingMask=0; presentation.depth=view.depth-1;
            presentation.clearFlags=CameraClearFlags.SolidColor; presentation.backgroundColor=Color.black;
            presentation.allowHDR=false; presentation.allowMSAA=false;
            view.orthographic = true; view.orthographicSize = 8.5f;
            view.allowHDR = false; view.allowMSAA = false;
            view.clearFlags = CameraClearFlags.SolidColor; view.backgroundColor = new Color(.018f,.015f,.018f);
            view.transform.rotation = Quaternion.Euler(30,-45,0);
            PositionCamera();
            UpdatePixelTarget();
            hex = BuildHex();
            Restart();
        }
        private void PositionCamera() => view.transform.position = focus - view.transform.forward * 30;
        private void UpdatePixelTarget()
        {
            float scale = UiScale;
            sceneRect = new Rect(270*scale,230*scale,Mathf.Max(1,Screen.width-270*scale),Mathf.Max(1,Screen.height-310*scale));
            // Integer pixel enlargement; point sampling prevents a blurry upscale.
            int zoom = Mathf.Max(1,Mathf.CeilToInt(sceneRect.height/240f));
            int width = Mathf.Max(1,Mathf.FloorToInt(sceneRect.width/zoom));
            int height = Mathf.Max(1,Mathf.FloorToInt(sceneRect.height/zoom));
            sceneRect = new Rect(sceneRect.x+(sceneRect.width-width*zoom)*.5f,sceneRect.y+(sceneRect.height-height*zoom)*.5f,width*zoom,height*zoom);
            if (pixelScene != null && pixelScene.width == width && pixelScene.height == height) return;
            view.targetTexture = null;
            if (pixelScene != null) { pixelScene.Release(); Destroy(pixelScene); }
            pixelScene = new RenderTexture(width,height,24,RenderTextureFormat.ARGB32)
            { name = "Ashen Bell pixel viewport", filterMode = FilterMode.Point, antiAliasing = 1, useMipMap = false };
            pixelScene.Create(); view.targetTexture = pixelScene;
            view.rect = new Rect(0,0,1,1); view.aspect = sceneRect.width/sceneRect.height;
        }
        private Ray PointerRay(Vector2 pointer)
        {
            return view.ViewportPointToRay(new Vector3((pointer.x-sceneRect.x)/sceneRect.width,(pointer.y-sceneRect.y)/sceneRect.height,0));
        }
        private Vector3 WorldToScreen(Vector3 position)
        {
            Vector3 p = view.WorldToViewportPoint(position);
            return new Vector3(sceneRect.x+p.x*sceneRect.width,sceneRect.y+p.y*sceneRect.height,p.z);
        }
        private void AddTorch(Vector3 position,Material iron)
        {
            Primitive("Iron torch stand",PrimitiveType.Cube,position+Vector3.up*.58f,new Vector3(.09f,1.16f,.09f),iron,world);
            Primitive("Brazier bowl",PrimitiveType.Cube,position+Vector3.up*1.14f,new Vector3(.3f,.16f,.3f),iron,world);
            var flame = new GameObject("Pixel flame",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            flame.transform.SetParent(world,false); flame.transform.position=position+Vector3.up*1.2f;
            flame.transform.rotation=view.transform.rotation; flame.sprite=art.Flame(); actors.Add(flame);
            var glow = new GameObject("Torchlight",typeof(Light)).GetComponent<Light>();
            glow.transform.SetParent(world,false); glow.transform.position=position+Vector3.up*1.5f;
            glow.type=LightType.Point; glow.color=new Color(1f,.47f,.17f); glow.range=5f; glow.intensity=2.1f;
            glow.shadows=LightShadows.None; torches.Add(glow);
        }
        private Vector3 Position(int id)
        {
            var c = battle.Cells[id]; return new Vector3(Mathf.Sqrt(3)*(c.Q+c.R*.5f),0,1.5f*c.R);
        }
        private Material MakeMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { color = color };
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness",0);
            materials.Add(mat); return mat;
        }
        private Transform Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material mat, Transform parent)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent, false); go.transform.position = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Destroy(go.GetComponent<Collider>()); return go.transform;
        }
        private void Restart()
        {
            StopAllCoroutines(); busy = false;
            if (world != null) { world.gameObject.SetActive(false); Destroy(world.gameObject); }
            foreach (var mat in materials) Destroy(mat);
            materials.Clear(); pieces.Clear(); tiles.Clear(); torches.Clear(); actors.Clear(); hover = -1;
            world = new GameObject("Generated Skirmish").transform; world.SetParent(transform);
            battle = new SkirmishBattle(loadouts,Random.Range(0,int.MaxValue));
            armedCard=null; armedEquipment=null; armedInnate=false;
            cardFeedbackUntil=0;
            var tileMat = MakeMaterial(stone); tileMat.mainTexture = art.Stone;
            var ruinMat = MakeMaterial(new Color(.52f,.48f,.42f)); ruinMat.mainTexture = art.Stone;
            var gold = MakeMaterial(new Color(.38f,.28f,.15f));
            var shadow = MakeMaterial(new Color(.06f,.045f,.04f));
            foreach (var c in battle.Cells)
            {
                var go = new GameObject("Hex " + c.Q + ", " + c.R, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(world); go.transform.position = Position(c.Id);
                go.GetComponent<MeshFilter>().sharedMesh = hex;
                var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = tileMat; tiles[c.Id] = renderer;
                if (c.Blocked)
                {
                    Vector3 at = Position(c.Id);
                    Primitive("Tomb foundation", PrimitiveType.Cube, at+Vector3.up*.14f, new Vector3(1.22f,.28f,1.10f), ruinMat, world);
                    Primitive("Weathered tomb", PrimitiveType.Cube, at+Vector3.up*.48f, new Vector3(.94f,.54f,.88f), ruinMat, world);
                    Primitive("Carved headstone", PrimitiveType.Cube, at+new Vector3(0,.93f,.21f), new Vector3(.74f,1.22f,.28f), ruinMat, world);
                    Primitive("Stone cap", PrimitiveType.Cube, at+new Vector3(0,1.56f,.21f), new Vector3(.85f,.12f,.38f), ruinMat, world);
                    Primitive("Engraved cross", PrimitiveType.Cube, at+new Vector3(0,1.13f,.058f), new Vector3(.09f,.48f,.015f), shadow, world);
                    Primitive("Cross arm", PrimitiveType.Cube, at+new Vector3(0,1.23f,.05f), new Vector3(.34f,.07f,.016f), shadow, world);
                    if (c.Id % 2 == 0) AddTorch(at+new Vector3(-.53f,0,-.42f),gold);
                }
            }
            foreach (var u in battle.Units)
            {
                var root = new GameObject(u.Name).transform; root.SetParent(world); root.position = Position(u.CellId);
                Primitive("Foot shadow", PrimitiveType.Cylinder,root.position+Vector3.up*.025f,new Vector3(.78f,.012f,.60f),shadow,root);
                var image = new GameObject(u.Role+" sprite",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                image.transform.SetParent(root,false); image.transform.localPosition=Vector3.up*.08f;
                image.transform.rotation=view.transform.rotation; image.sprite=art.Character(SpriteRole(u));
                actors.Add(image);
                pieces[u] = root;
            }
            selected = battle.Units[0]; hint = "Defeat every revenant. Keep at least one hero alive.";
            Refresh();
        }
        private void Refresh()
        {
            costs.Clear();
            if (selected != null && selected.Alive && !battle.EnemyTurn && !battle.Finished)
                foreach (var c in battle.Cells)
                {
                    var path = battle.Path(selected,c.Id);
                    if (path.Count > 0 && path.Count <= selected.AP) costs[c.Id] = path.Count;
                }
            foreach (var pair in pieces) pair.Value.gameObject.SetActive(pair.Key.Alive);
            Paint();
        }
        private void Paint()
        {
            var path = selected != null && hover >= 0 && costs.ContainsKey(hover) ? battle.Path(selected,hover) : new List<int>();
            var block = new MaterialPropertyBlock();
            foreach (var c in battle.Cells)
            {
                Color color = (c.Blocked ? stone*.75f : stone) * (1f + ((c.Id*17)%9-4)*.018f);
                if (costs.ContainsKey(c.Id)) color = new Color(.57f,.62f,.43f);
                if (path.Contains(c.Id)) color = new Color(.77f,.74f,.47f);
                var occupant = battle.At(c.Id);
                if (selected != null && battle.CanAttack(selected,occupant)) color = new Color(.72f,.30f,.22f);
                if(armedCard!=null)
                {
                    color=c.Blocked ? stone*.75f : stone;
                    if(ArmedError(c.Id)==null)
                        color=armedCard.Target==CardTarget.Enemy ? new Color(.83f,.30f,.20f) : new Color(.70f,.77f,.48f);
                }
                if (selected != null && selected.Alive && selected.CellId == c.Id) color = new Color(.95f,.73f,.34f);
                if (c.Id == hover) color = Color.Lerp(color,Color.white,.25f);
                block.SetColor("_BaseColor",color); block.SetColor("_Color",color); tiles[c.Id].SetPropertyBlock(block);
            }
        }
        private bool OverUI(Vector2 screen)
        {
            Vector2 guiPoint=new Vector2(screen.x/UiScale,(Screen.height-screen.y)/UiScale);
            if(Time.unscaledTime<cardFeedbackUntil && new Rect(280,83,Screen.width/UiScale-300,62).Contains(guiPoint)) return true;
            return preparing || !sceneRect.Contains(screen) || battle.Finished;
        }
        private SkirmishBattle.Unit CharacterAtPointer(Vector2 pointer)
        {
            var ray=PointerRay(pointer); SkirmishBattle.Unit closest=null; float nearest=float.PositiveInfinity;
            foreach(var pair in pieces)
            {
                if(!pair.Key.Alive) continue;
                var renderer=pair.Value.GetComponentInChildren<SpriteRenderer>();
                if(renderer==null || renderer.sprite==null) continue;
                var plane=new Plane(renderer.transform.forward,renderer.transform.position);
                if(!plane.Raycast(ray,out float distance) || distance>=nearest) continue;
                Vector3 local=renderer.transform.InverseTransformPoint(ray.GetPoint(distance));
                var sprite=renderer.sprite;
                int x=Mathf.FloorToInt(local.x*sprite.pixelsPerUnit+sprite.pivot.x);
                int y=Mathf.FloorToInt(local.y*sprite.pixelsPerUnit+sprite.pivot.y);
                if(x<0 || y<0 || x>=sprite.rect.width || y>=sprite.rect.height) continue;
                if(sprite.texture.GetPixel((int)sprite.rect.x+x,(int)sprite.rect.y+y).a<.1f) continue;
                nearest=distance; closest=pair.Key;
            }
            return closest;
        }
        private void Update()
        {
            if (battle == null) return;
            UpdatePixelTarget();
            for (int i=0;i<torches.Count;i++)
                torches[i].intensity = 2.1f + Mathf.Sin(Time.time*8.1f+i*2.3f)*.22f + Mathf.Sin(Time.time*17.3f+i)*.09f;
            foreach (var actor in actors)
                actor.sortingOrder = -(int)(Vector3.Dot(actor.transform.position,view.transform.forward)*100);
            if (preparing) return;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0)-(keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
                float z = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0)-(keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
                Vector3 right = view.transform.right; right.y=0;
                Vector3 up = view.transform.up; up.y=0;
                focus += (right.normalized*x+up.normalized*z)*Time.deltaTime*10; focus.x = Mathf.Clamp(focus.x,0,26); focus.z = Mathf.Clamp(focus.z,-4,18); PositionCamera();
                if (keyboard.escapeKey.wasPressedThisFrame) { armedCard=null; armedEquipment=null; armedInnate=false; Paint(); }
                if (keyboard.spaceKey.wasPressedThisFrame) BeginEnemyTurn();
                if (!busy && !battle.EnemyTurn && !battle.Finished && keyboard.tabKey.wasPressedThisFrame)
                {
                    var squad = battle.Units.Where(u => u.Alive && !u.Enemy).ToList();
                    selected = squad[(squad.IndexOf(selected)+1)%squad.Count]; armedCard=null; armedEquipment=null; armedInnate=false; Refresh();
                }
            }
            var mouse = Mouse.current; if (mouse == null) return;
            Vector2 pointer = mouse.position.ReadValue();
            if (!OverUI(pointer)) view.orthographicSize = Mathf.Clamp(view.orthographicSize-mouse.scroll.ReadValue().y*.006f,6,19);
            int next = -1;
            if (!OverUI(pointer))
            {
                var ray = PointerRay(pointer); var plane = new Plane(Vector3.up,Vector3.zero);
                if (plane.Raycast(ray,out float distance))
                {
                    Vector3 point = ray.GetPoint(distance); float nearest = .95f*.95f;
                    foreach (var c in battle.Cells)
                    {
                        float d = (Position(c.Id)-point).sqrMagnitude;
                        if (d < nearest) { nearest = d; next = c.Id; }
                    }
                }
                // Only actual sprite pixels select a character; do not snap to a nearby unit.
                var pointedCharacter=CharacterAtPointer(pointer);
                if(pointedCharacter!=null) next=pointedCharacter.CellId;
            }
            if (hover != next) { hover = next; Paint(); }
            if(hover<0 && armedCard!=null && !busy && !battle.EnemyTurn && !battle.Finished &&
                !OverUI(pointer) && mouse.leftButton.wasPressedThisFrame)
            { UseArmedCard(-1); return; }
            if (hover >= 0 && !busy && !battle.EnemyTurn && !battle.Finished && mouse.leftButton.wasPressedThisFrame)
            {
                var target = battle.At(hover);
                if(armedCard!=null) { UseArmedCard(hover); return; }
                if (target != null && !target.Enemy) { selected = target; armedCard=null; armedEquipment=null; armedInnate=false; Refresh(); }
                else if (target != null && selected != null)
                {
                    if (battle.Attack(selected,target)) { ShowCardFeedback(battle.Log[0],false); StartCoroutine(Strike(selected,target)); Refresh(); }
                    else { hint=battle.EquipmentError(selected,HeroEquipment.Weapon(selected.Inventory).Id,target.CellId); ShowCardFeedback(hint+" AP kept.",true); }
                }
                else if (selected != null && costs.ContainsKey(hover))
                {
                    var path = battle.Path(selected,hover);
                    if (battle.Move(selected,hover)) StartCoroutine(Walk(selected,path));
                }
            }
        }
        private IEnumerator Walk(SkirmishBattle.Unit unit, List<int> path)
        {
            busy = true; costs.Clear(); Paint();
            foreach (int id in path)
            {
                Vector3 start = pieces[unit].position, end = Position(id);
                for (float t = 0; t < 1; t += Time.deltaTime*6)
                { pieces[unit].position = Vector3.Lerp(start,end,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.1f; yield return null; }
                pieces[unit].position = end;
            }
            busy = false; Refresh();
        }
        private IEnumerator Strike(SkirmishBattle.Unit attacker, SkirmishBattle.Unit target)
        {
            busy = true;
            Vector3 origin = pieces[attacker].position;
            Vector3 delta = (Position(target.CellId)-origin).normalized*.35f;
            for (float t = 0; t < 1; t += Time.deltaTime*4)
            { pieces[attacker].position = origin+delta*Mathf.Sin(t*Mathf.PI); yield return null; }
            pieces[attacker].position = origin; busy = false; Refresh();
        }
        private void BeginEnemyTurn()
        {
            if (preparing || busy || battle == null || battle.EnemyTurn || battle.Finished) return;
            armedCard=null; armedEquipment=null; armedInnate=false;
            StartCoroutine(EnemyTurn());
        }
        private IEnumerator EnemyTurn()
        {
            busy = true; battle.EndTurn(); Refresh();
            foreach (var u in battle.Units.Where(u => u.Enemy && u.Alive).ToList())
            {
                for (int action = 0; action < SkirmishBattle.MaxAP && !battle.Finished; action++)
                {
                    yield return new WaitForSeconds(.28f);
                    int old = u.CellId;
                    if (!battle.EnemyAction(u)) break;
                    if (old != u.CellId)
                    {
                        yield return Walk(u,new List<int> { u.CellId }); busy = true;
                    }
                    Refresh();
                }
            }
            if (!battle.Finished) battle.EndTurn();
            if (selected == null || !selected.Alive) selected = battle.Units.FirstOrDefault(u => !u.Enemy && u.Alive);
            busy = false; Refresh();
        }
        private void Styles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold };
            body = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            small = new GUIStyle(body) { fontSize = 12 };
            button = new GUIStyle(GUI.skin.button) { fontSize = 14, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(12,8,6,6), border = new RectOffset(2,2,2,2) };
            title.normal.textColor = new Color(.82f,.69f,.44f);
            body.normal.textColor = new Color(.79f,.74f,.64f);
            small.normal.textColor = new Color(.66f,.61f,.52f);
            button.normal.background = art.Button; button.hover.background = art.Button; button.active.background = art.Button;
            button.normal.textColor = new Color(.84f,.77f,.62f);
            button.hover.textColor = new Color(1f,.87f,.59f); button.active.textColor = Color.white;
        }
        private void Panel(Rect rect)
        {
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(rect,art.Panel,new Rect(0,0,rect.width/64,rect.height/64));
            GUI.color = new Color(.38f,.29f,.16f);
            GUI.DrawTexture(new Rect(rect.x,rect.y,rect.width,1),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x,rect.yMax-1,rect.width,1),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x,rect.y,1,rect.height),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax-1,rect.y,1,rect.height),Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        private void OnGUI()
        {
            if (battle == null) return;
            if (pixelScene != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.blackTexture);
                GUI.DrawTexture(new Rect(sceneRect.x,Screen.height-sceneRect.yMax,sceneRect.width,sceneRect.height),pixelScene,ScaleMode.StretchToFill,false);
            }
            Styles(); float scale = UiScale, width = Screen.width/scale, height = Screen.height/scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(scale,scale,1));
            if(preparing) { DrawPreparation(width,height); GUI.matrix=Matrix4x4.identity; return; }
            Panel(new Rect(0,0,width,76));
            GUI.Label(new Rect(22,10,550,34),"GOTHIC TACTICS  /  THE ASHEN BELL",title);
            GUI.Label(new Rect(22,45,720,24),"A ruined sanctuary. A shared player turn.  •  Defeat every revenant.",small);
            GUI.Label(new Rect(width-280,15,265,35),"ROUND " + battle.Round + "  /  " + (battle.EnemyTurn ? "REVENANTS" : "YOUR TURN"),body);
            Panel(new Rect(0,80,260,height-310));
            GUI.Label(new Rect(18,94,230,25),"YOUR HUNTERS",body);
            float y = 126;
            foreach (var u in battle.Units.Where(u => !u.Enemy))
            {
                GUI.enabled = u.Alive && !busy && !battle.EnemyTurn && !battle.Finished;
                GUI.backgroundColor = u == selected ? new Color(.85f,.66f,.37f) : Color.white;
                if (GUI.Button(new Rect(14,y,230,46),u.Name + "  /  " + u.Role + "\n" + (u.Alive ? u.HP+"/"+u.MaxHP+" HP  "+u.Shield+" SH  "+u.AP+"/6 AP"+(u.BleedTurns>0 ? " BLEED "+u.BleedTurns : "") : "FALLEN"),button))
                { selected = u; armedCard=null; armedEquipment=null; armedInnate=false; Refresh(); }
                y += 52;
            }
            GUI.backgroundColor = Color.white; GUI.enabled = true;
            if(selected?.Hero!=null)
            {
                GUI.Label(new Rect(18,y+5,224,22),selected.Hero.Affinities+" / "+selected.Hero.Resource+" "+selected.Resource+"/3",small); y+=30;
                GUI.Label(new Rect(18,y,224,54),selected.Hero.Passive,small); y+=58;
                GUI.enabled=!busy && !battle.EnemyTurn && battle.CanAct(selected) && !selected.InnateUsed && selected.AP>=1;
                var innate=HeroCards.Innate(selected.Hero.Innate);
                if(GUI.Button(new Rect(14,y,230,34),innate.Name+" / 1 AP",button)) Arm(innate,true);
                GUI.enabled=true; y+=38;
                y=DrawEquipmentActions(y);
            }
            foreach(string line in battle.Log.Take(Mathf.Max(0,(int)((height-240-y)/38))))
            { GUI.Label(new Rect(18,y,224,36),line,small); y+=38; }
            foreach (var u in battle.Units.Where(u => u.Alive))
            {
                Vector3 p = WorldToScreen(pieces[u].position+view.transform.up*2.65f);
                if (p.z <= 0) continue;
                float px = p.x/scale, py = (Screen.height-p.y)/scale;
                if (px < 300 || py < 90 || py > height-235) continue;
                Panel(new Rect(px-48,py,96,29));
                GUI.Label(new Rect(px-44,py+1,90,20),u.Name,small);
                GUI.color = u.Enemy ? red : teal; GUI.DrawTexture(new Rect(px-44,py+23,88f*u.HP/u.MaxHP,3),Texture2D.whiteTexture); GUI.color = Color.white;
            }
            DrawHand(width,height);
            if (battle.Finished)
            {
                Panel(new Rect(width/2-230,height/2-120,460,240));
                GUI.Label(new Rect(width/2-205,height/2-96,410,38),battle.Victory ? "THE SANCTUARY IS YOURS" : "THE VIGIL HAS ENDED",title);
                GUI.Label(new Rect(width/2-205,height/2-43,400,70),battle.Victory ? "The bell will sound again.\nSurvivors: "+battle.Units.Count(u => !u.Enemy && u.Alive)+" / "+loadouts.Count+"   •   Rounds: "+battle.Round : "Your hunters have fallen. Try holding the gaps, guarding, and focusing your attacks.",body);
                if (GUI.Button(new Rect(width/2-205,height/2+50,410,45),"RETURN TO HEROES & DECKS",button)) { preparing=true; armedCard=null; armedEquipment=null; }
            }
            GUI.matrix = Matrix4x4.identity;
        }
        private Mesh BuildHex()
        {
            var vertices = new Vector3[7]; var uv = new Vector2[7]; uv[0]=new Vector2(.5f,.5f); var triangles = new int[18];
            for (int i = 0; i < 6; i++)
            {
                float angle = (60*i+30)*Mathf.Deg2Rad;
                vertices[i+1] = new Vector3(Mathf.Cos(angle)*.99f,0,Mathf.Sin(angle)*.99f);
                uv[i+1] = new Vector2(vertices[i+1].x*.5f+.5f,vertices[i+1].z*.5f+.5f);
                triangles[i*3] = 0; triangles[i*3+1] = i == 5 ? 1 : i+2; triangles[i*3+2] = i+1;
            }
            var mesh = new Mesh { name = "Skirmish Hex", vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateNormals(); return mesh;
        }
        private void OnDestroy()
        {
            foreach (var mat in materials) if (mat != null) Destroy(mat);
            if (hex != null) Destroy(hex);
            if (view != null) view.targetTexture = null;
            if (presentation != null) Destroy(presentation.gameObject);
            if (pixelScene != null) { pixelScene.Release(); Destroy(pixelScene); }
            art?.Dispose();
        }
    }
}
