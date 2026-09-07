using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GothicTactics.Skirmish
{
    public sealed class SkirmishController : MonoBehaviour
    {
        private SkirmishBattle battle;
        private SkirmishBattle.Unit selected;
        private Camera view;
        private Transform world;
        private readonly Dictionary<int, Renderer> tiles = new Dictionary<int, Renderer>();
        private readonly Dictionary<SkirmishBattle.Unit, Transform> pieces = new Dictionary<SkirmishBattle.Unit, Transform>();
        private readonly List<Material> materials = new List<Material>();
        private readonly Dictionary<int, int> costs = new Dictionary<int, int>();
        private readonly Color stone = new Color(.18f,.23f,.25f), teal = new Color(.20f,.72f,.66f), red = new Color(.8f,.26f,.24f);
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
            view.orthographic = true; view.orthographicSize = 9.5f;
            view.clearFlags = CameraClearFlags.SolidColor; view.backgroundColor = new Color(.035f,.055f,.065f);
            view.transform.rotation = Quaternion.Euler(57,0,0);
            PositionCamera();
            hex = BuildHex();
            Restart();
        }
        private void PositionCamera() => view.transform.position = focus - view.transform.forward * 30;
        private Vector3 Position(int id)
        {
            var c = battle.Cells[id]; return new Vector3(Mathf.Sqrt(3)*(c.Q+c.R*.5f),0,1.5f*c.R);
        }
        private Material MakeMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { color = color };
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
            materials.Clear(); pieces.Clear(); tiles.Clear(); hover = -1;
            world = new GameObject("Generated Skirmish").transform; world.SetParent(transform);
            battle = new SkirmishBattle();
            var tileMat = MakeMaterial(stone); var ruinMat = MakeMaterial(new Color(.26f,.29f,.3f));
            var gold = MakeMaterial(new Color(.73f,.55f,.27f));
            foreach (var c in battle.Cells)
            {
                var go = new GameObject("Hex " + c.Q + ", " + c.R, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(world); go.transform.position = Position(c.Id);
                go.GetComponent<MeshFilter>().sharedMesh = hex;
                var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = tileMat; tiles[c.Id] = renderer;
                if (c.Blocked)
                {
                    Primitive("Ruin plinth", PrimitiveType.Cube, Position(c.Id)+Vector3.up*.25f, new Vector3(1.05f,.5f,.9f), ruinMat, world);
                    Primitive("Broken pillar", PrimitiveType.Cube, Position(c.Id)+Vector3.up*.95f, new Vector3(.6f,1.6f,.55f), ruinMat, world);
                    Primitive("Iron band", PrimitiveType.Cube, Position(c.Id)+Vector3.up*1.35f, new Vector3(.68f,.12f,.63f), gold, world);
                }
            }
            foreach (var u in battle.Units)
            {
                var root = new GameObject(u.Name).transform; root.SetParent(world); root.position = Position(u.CellId);
                var mat = MakeMaterial(u.Enemy ? red : teal);
                var armour = MakeMaterial(u.Enemy ? new Color(.26f,.12f,.16f) : new Color(.12f,.28f,.3f));
                Primitive("Base", PrimitiveType.Cylinder, root.position+Vector3.up*.1f, new Vector3(.85f,.10f,.85f), mat,root);
                Primitive("Body", PrimitiveType.Capsule, root.position+Vector3.up*.75f, new Vector3(.48f,.57f,.48f), armour,root);
                Primitive("Helm", PrimitiveType.Sphere, root.position+Vector3.up*1.42f, Vector3.one*.4f, mat,root);
                if (u.Range == 1)
                {
                    Primitive("Blade", PrimitiveType.Cube, root.position+new Vector3(.4f,.95f,0), new Vector3(.1f,1.1f,.1f),gold,root);
                    Primitive("Shield", PrimitiveType.Cube, root.position+new Vector3(-.34f,.8f,-.08f),new Vector3(.18f,.65f,.5f),mat,root);
                }
                else
                {
                    Primitive("Weapon", PrimitiveType.Cube,root.position+new Vector3(.32f,.85f,0),new Vector3(.12f,1.35f,.12f),gold,root);
                    Primitive("Focus",PrimitiveType.Sphere,root.position+new Vector3(.32f,1.6f,0),Vector3.one*.25f,mat,root);
                }
                pieces[u] = root;
            }
            selected = battle.Units[0]; hint = "Destroy all four revenants. Keep at least one hunter alive.";
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
                Color color = c.Blocked ? stone*.65f : stone;
                if (costs.ContainsKey(c.Id)) color = new Color(.15f,.38f,.36f);
                if (path.Contains(c.Id)) color = teal*.8f;
                var occupant = battle.At(c.Id);
                if (selected != null && battle.CanAttack(selected,occupant)) color = new Color(.55f,.19f,.17f);
                if (selected != null && selected.Alive && selected.CellId == c.Id) color = new Color(.7f,.53f,.25f);
                if (c.Id == hover) color = Color.Lerp(color,Color.white,.25f);
                block.SetColor("_BaseColor",color); block.SetColor("_Color",color); tiles[c.Id].SetPropertyBlock(block);
            }
        }
        private bool OverUI(Vector2 screen)
        {
            float scale = UiScale, x = screen.x/scale, y = (Screen.height-screen.y)/scale;
            return x < 270 || y < 80 || y > Screen.height/scale-110 || battle.Finished;
        }
        private void Update()
        {
            if (battle == null) return;
            float scale = UiScale;
            view.rect = new Rect(270*scale/Screen.width,110*scale/Screen.height,
                1-270*scale/Screen.width,1-190*scale/Screen.height);
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0)-(keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
                float z = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0)-(keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
                focus += new Vector3(x,0,z)*Time.deltaTime*10; focus.x = Mathf.Clamp(focus.x,0,26); focus.z = Mathf.Clamp(focus.z,-4,18); PositionCamera();
                if (keyboard.spaceKey.wasPressedThisFrame) BeginEnemyTurn();
                if (!busy && !battle.EnemyTurn && !battle.Finished && keyboard.tabKey.wasPressedThisFrame)
                {
                    var squad = battle.Units.Where(u => u.Alive && !u.Enemy).ToList();
                    selected = squad[(squad.IndexOf(selected)+1)%squad.Count]; Refresh();
                }
            }
            var mouse = Mouse.current; if (mouse == null) return;
            Vector2 pointer = mouse.position.ReadValue();
            if (!OverUI(pointer)) view.orthographicSize = Mathf.Clamp(view.orthographicSize-mouse.scroll.ReadValue().y*.006f,6,19);
            int next = -1;
            if (!OverUI(pointer))
            {
                var ray = view.ScreenPointToRay(pointer); var plane = new Plane(Vector3.up,Vector3.zero);
                if (plane.Raycast(ray,out float distance))
                {
                    Vector3 point = ray.GetPoint(distance); float nearest = .95f*.95f;
                    foreach (var c in battle.Cells)
                    {
                        float d = (Position(c.Id)-point).sqrMagnitude;
                        if (d < nearest) { nearest = d; next = c.Id; }
                    }
                }
                // Let clicks on the visible character select its feet tile as well.
                float closest = 25*UiScale;
                foreach (var u in battle.Units.Where(u => u.Alive))
                {
                    Vector3 projected = view.WorldToScreenPoint(pieces[u].position+Vector3.up*.9f);
                    float distanceToPointer = Vector2.Distance(pointer,new Vector2(projected.x,projected.y));
                    if (projected.z > 0 && distanceToPointer < closest)
                    { closest = distanceToPointer; next = u.CellId; }
                }
            }
            if (hover != next) { hover = next; Paint(); }
            if (hover >= 0 && !busy && !battle.EnemyTurn && !battle.Finished && mouse.leftButton.wasPressedThisFrame)
            {
                var target = battle.At(hover);
                if (target != null && !target.Enemy) { selected = target; Refresh(); }
                else if (target != null && selected != null)
                {
                    if (battle.Attack(selected,target)) { StartCoroutine(Strike(selected,target)); Refresh(); }
                    else hint = "Attack unavailable: needs 2 AP, range and an unobstructed shot.";
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
            if (busy || battle == null || battle.EnemyTurn || battle.Finished) return;
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
            button = new GUIStyle(GUI.skin.button) { fontSize = 14, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(12,8,6,6) };
        }
        private void Panel(Rect rect)
        {
            GUI.color = new Color(.055f,.085f,.10f,.97f); GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color = Color.white;
        }
        private void OnGUI()
        {
            if (battle == null) return;
            Styles(); float scale = UiScale, width = Screen.width/scale, height = Screen.height/scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(scale,scale,1));
            Panel(new Rect(0,0,width,76));
            GUI.Label(new Rect(22,10,550,34),"GOTHIC TACTICS  /  THE ASHEN BELL",title);
            GUI.Label(new Rect(22,45,720,24),"A ruined sanctuary. Three hunters. One last vigil.  •  Defeat every revenant.",small);
            GUI.Label(new Rect(width-280,15,265,35),"ROUND " + battle.Round + "  /  " + (battle.EnemyTurn ? "REVENANTS" : "YOUR TURN"),body);
            Panel(new Rect(0,80,260,height-190));
            GUI.Label(new Rect(18,94,230,25),"YOUR HUNTERS",body);
            float y = 126;
            foreach (var u in battle.Units.Where(u => !u.Enemy))
            {
                GUI.enabled = u.Alive && !busy && !battle.EnemyTurn && !battle.Finished;
                GUI.backgroundColor = u == selected ? teal : Color.white;
                if (GUI.Button(new Rect(14,y,230,64),u.Name + "  /  " + u.Role + "\n" + (u.Alive ? u.HP+"/"+u.MaxHP+" HP    "+u.AP+"/6 AP" : "FALLEN"),button))
                { selected = u; Refresh(); }
                y += 72;
            }
            GUI.backgroundColor = Color.white; GUI.enabled = true;
            if (selected != null)
            {
                y += 8;
                GUI.Label(new Rect(18,y,230,48),selected.Damage+" damage  •  "+selected.Range+" hex range\nAttack: 2 AP  /  Move: 1 AP per hex",small); y += 55;
                GUI.enabled = !busy && battle.CanAct(selected) && !battle.EnemyTurn && selected.AP > 0;
                if (GUI.Button(new Rect(14,y,230,37),"Guard  /  spend remaining AP",button)) { battle.Guard(selected); Refresh(); }
                y += 44;
                GUI.enabled = !busy && battle.CanAct(selected) && !battle.EnemyTurn && selected.AP >= 2 && selected.Tonics > 0 && selected.HP < selected.MaxHP;
                if (GUI.Button(new Rect(14,y,230,37),"Tonic +6 HP  /  2 AP  /  "+selected.Tonics+" left",button)) { battle.Heal(selected); Refresh(); }
                GUI.enabled = true; y += 53;
            }
            GUI.Label(new Rect(18,y,224,50),"Teal: movement  •  Red: attack\nGold: selected hunter",small); y += 52;
            foreach (string line in battle.Log.Take(Mathf.Max(0,(int)((height-125-y)/40))))
            { GUI.Label(new Rect(18,y,224,38),line,small); y += 40; }
            foreach (var u in battle.Units.Where(u => u.Alive))
            {
                Vector3 p = view.WorldToScreenPoint(pieces[u].position+Vector3.up*2);
                if (p.z <= 0) continue;
                float px = p.x/scale, py = (Screen.height-p.y)/scale;
                if (px < 300 || py < 90 || py > height-120) continue;
                Panel(new Rect(px-48,py,96,29));
                GUI.Label(new Rect(px-44,py+1,90,20),u.Name,small);
                GUI.color = u.Enemy ? red : teal; GUI.DrawTexture(new Rect(px-44,py+23,88f*u.HP/u.MaxHP,3),Texture2D.whiteTexture); GUI.color = Color.white;
            }
            Panel(new Rect(0,height-106,width,106));
            string context = hint;
            if (hover >= 0 && selected != null)
            {
                var target = battle.At(hover);
                if (target != null) context = target.Name+"  /  "+target.HP+" HP"+(battle.CanAttack(selected,target) ? "  •  Click: "+battle.AttackDamage(selected,target)+" damage / 2 AP" : "");
                else if (costs.TryGetValue(hover,out int cost)) context = "Click to move  /  "+cost+" AP  /  "+(selected.AP-cost)+" AP remaining";
                else context = battle.Cells[hover].Blocked ? "Ruins block movement and ranged sight." : "This hex is out of reach.";
            }
            GUI.Label(new Rect(22,height-91,width-330,40),context,body);
            GUI.Label(new Rect(22,height-43,width-330,26),"CLICK select / move / attack    •    TAB next hunter    •    WASD pan    •    SCROLL zoom    •    SPACE end turn",small);
            GUI.enabled = !busy && !battle.EnemyTurn && !battle.Finished;
            if (GUI.Button(new Rect(width-265,height-85,240,56),"END TURN  →",button)) BeginEnemyTurn();
            GUI.enabled = true;
            if (battle.Finished)
            {
                Panel(new Rect(width/2-230,height/2-120,460,240));
                GUI.Label(new Rect(width/2-205,height/2-96,410,38),battle.Victory ? "THE SANCTUARY IS YOURS" : "THE VIGIL HAS ENDED",title);
                GUI.Label(new Rect(width/2-205,height/2-43,400,70),battle.Victory ? "The bell will sound again.\nSurvivors: "+battle.Units.Count(u => !u.Enemy && u.Alive)+" / 3   •   Rounds: "+battle.Round : "Your hunters have fallen. Try holding the gaps, guarding, and focusing your attacks.",body);
                if (GUI.Button(new Rect(width/2-205,height/2+50,410,45),"PLAY AGAIN",button)) Restart();
            }
            GUI.matrix = Matrix4x4.identity;
        }
        private Mesh BuildHex()
        {
            var vertices = new Vector3[7]; var triangles = new int[18];
            for (int i = 0; i < 6; i++)
            {
                float angle = (60*i+30)*Mathf.Deg2Rad;
                vertices[i+1] = new Vector3(Mathf.Cos(angle)*.95f,0,Mathf.Sin(angle)*.95f);
                triangles[i*3] = 0; triangles[i*3+1] = i == 5 ? 1 : i+2; triangles[i*3+2] = i+1;
            }
            var mesh = new Mesh { name = "Skirmish Hex", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals(); return mesh;
        }
        private void OnDestroy()
        {
            foreach (var mat in materials) if (mat != null) Destroy(mat);
            if (hex != null) Destroy(hex);
        }
    }
}
