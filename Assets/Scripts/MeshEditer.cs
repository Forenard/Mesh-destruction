using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using URandom=UnityEngine.Random;
using Random=System.Random;
[RequireComponent (typeof(MeshRenderer))]
[RequireComponent (typeof(MeshFilter))]
[RequireComponent (typeof(MeshCollider))]
[RequireComponent (typeof(Rigidbody))]

public class MeshEditer : MonoBehaviour
{
    //Randomのコンフリクト解消
    
    
    /************************************************
    注意点
    ・点は全部indexの順序の番号の整数値を指している
    頂点はほぼ使わない
    ************************************************/

    //なんとなく保持する変数ども-----------------------------------
    
    
    //頂点
    List<Vector3> vertice=new List<Vector3>();
    //頂点の順序
    List<int> index=new List<int>();
    
    //グラフ

    //三角形(頂点3つをまとめただけ)
    List<int[]> triangle=new List<int[]>();

    //辺はint[2]で頂点2つで表す
    //辺のリスト{番号->[頂点->頂点]の繋がり}これは
    List<int[]> edge=new List<int[]>();
    //探知用の辺のリスト{[頂点->頂点]->[対応する番号ら]の繋がり}
    Dictionary<int[],List<int>> edge_from=new Dictionary<int[],List<int>>();
    //辺の繋がりのグラフ{edgeの番号->edgeの番号の繋がり}
    List<List<int>> edge_graph=new List<List<int>>();
    /*
    このサイズが0だったら縁1だったらそこがつながった辺
    */
    //頂点の連結のグラフ(無向になるかも)
    List<List<int>> index_graph=new List<List<int>>();
    
    //分割後のやつ
    List<int> newIndex=new List<int>();
    Vector3[] newVertice;
    //最後に出すオブジェクト
    GameObject[] result_objects;
    //なんとなく保持する変数ども-----------------------------------
    
    //ここからSerializeField--------------------------------------

    [SerializeField, HeaderAttribute ("破壊後の破片の個数")]
    int brokenum=4;
    [SerializeField, HeaderAttribute ("破片の飛び散る速度")]
    float speed=5;

    //ここからSerializeField--------------------------------------


    void Mesh_Div(){
        List<Vector3> newIndex_vec=new List<Vector3>();
        Dictionary<Vector3,int> vertice_dict=new Dictionary<Vector3,int>();
        

        //分割
        for (int i = 0; i < index.Count/3; i++)
        {
            Vector3[] not_mid=new Vector3[3];
            Vector3[] mid=new Vector3[3];
            for (int j = 0; j < 3; j++)
            {
                Vector3 chu=(vertice[index[i*3+j]]+vertice[index[i*3+(j+1)%3]])/2f;
                not_mid[j]=vertice[index[i*3+j]];
                mid[j]=chu;
            }
            for (int j = 0; j < 3; j++){
                //分割
                newIndex_vec.Add(not_mid[j]);
                newIndex_vec.Add(mid[j]);
                newIndex_vec.Add(mid[(j+2)%3]);
            }
            //分割の中間
            newIndex_vec.Add(mid[0]);
            newIndex_vec.Add(mid[1]);
            newIndex_vec.Add(mid[2]);
        }

        //vecからindex,verticeに変換
        int _size=newIndex_vec.Count;

        for (int i = 0; i < _size/3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                int _nowi=i*3+j;
                if(!vertice_dict.ContainsKey(newIndex_vec[_nowi])){
                    vertice_dict.Add(newIndex_vec[_nowi],vertice_dict.Count);
                }
                newIndex.Add(vertice_dict[newIndex_vec[_nowi]]);
            }
        }
        newVertice=new Vector3[vertice_dict.Count];
        foreach (Vector3 _vec in vertice_dict.Keys)
        {
            newVertice[vertice_dict[_vec]]=_vec;
            
        }
        //これで分割された
    }
    void Test_Obj(){
        
    }

    void Create_Obj(){
        //まず全て分割した後を保持
        Vector3[,] vertices=new Vector3[newIndex.Count/3,3];
        List<List<List<Vector3>>> result_vertices=new List<List<List<Vector3>>>();
        
        for (int i = 0; i < newIndex.Count/3; i++)
        {
            vertices[i,0]=newVertice[newIndex[i*3+0]];
            vertices[i,1]=newVertice[newIndex[i*3+1]];
            vertices[i,2]=newVertice[newIndex[i*3+2]];
            
        }
        
        
        //メッシュを結合(ひび割れを作る為)
        //ランダムに母点を選択
        int[] rand_point=new int[newVertice.Length];
        for (int i = 0; i < rand_point.Length; i++)
        {
            rand_point[i]=i;
        }
        rand_point=rand_point.OrderBy(a=>Guid.NewGuid()).ToArray();
        
        result_objects=new GameObject[brokenum];
        //リザルトのメッシュを計算、保持
        for (int i = 0; i < brokenum; i++)
        {
            result_vertices.Add(new List<List<Vector3>>(){});
        }

        for (int i = 0; i < newIndex.Count/3; i++)
        {
            Vector3 midvec=(vertices[i,0]+vertices[i,1]+vertices[i,2])/3f;
        
            Vector3 top_point=newVertice[rand_point[0]];
            int now_point=rand_point[0];
            int now_def=0;
            float now_dis=(midvec-top_point).sqrMagnitude;
            for (int j = 1; j < brokenum; j++)
            {
                top_point=newVertice[rand_point[j]];
                float _dis=(midvec-top_point).sqrMagnitude;
                if(_dis<now_dis){
                    now_point=rand_point[j];
                    now_def=j;
                    now_dis=_dis;
                }
            }
            List<Vector3> _ver=new List<Vector3>();
            _ver.Add(vertices[i,0]);
            _ver.Add(vertices[i,1]);
            _ver.Add(vertices[i,2]);
            
            result_vertices[now_def].Add(_ver);
        }
        
        //リザルトのメッシュをオブジェクトに変換
        for (int i = 0; i < brokenum; i++)
        {
            int[] _ind=new int[result_vertices[i].Count*3];
            Dictionary<Vector3,int> _dict=new Dictionary<Vector3,int>();
            for (int j = 0; j < result_vertices[i].Count; j++)
            {
                for (int l = 0; l < 3; l++)
                {
                    Vector3 _vert_tmp=result_vertices[i][j][l];
                    if(!_dict.ContainsKey(_vert_tmp)){
                        _dict.Add(_vert_tmp,_dict.Count);
                    }
                    _ind[j*3+l]=_dict[_vert_tmp];
                }
            }
            Vector3[] _vert=new Vector3[_dict.Count];
            foreach (Vector3 _v in _dict.Keys)
            {
                _vert[_dict[_v]]=_v;
            }
            
            //obj作成
            GameObject clone = GameObject.Instantiate( gameObject ) as GameObject;
            clone.GetComponent<MeshEditer>().enabled=false;
            clone.transform.position=this.transform.position;
            var _mesh = new Mesh ();
            _mesh.vertices = _vert;
            _mesh.triangles = _ind;
            _mesh.RecalculateNormals();
            clone.GetComponent<MeshFilter>().sharedMesh=_mesh;
            clone.GetComponent<MeshCollider>().sharedMesh=_mesh;
            
            clone.GetComponent<MeshRenderer>().enabled=false;
            result_objects[i]=clone;
        }
        
        
    }

    void Add_Force(){
        this.GetComponent<MeshRenderer>().enabled=false;
        foreach (GameObject fragment in result_objects)
        {
            float _x=URandom.Range(0f,360f);
            float _y=URandom.Range(0f,360f);
            float _z=URandom.Range(0f,360f);
            Vector3 rand_vec=Quaternion.Euler(_x,_y,_z)*(new Vector3(1,1,1))*speed;
            fragment.GetComponent<MeshRenderer>().enabled=true;
            
            fragment.GetComponent<Rigidbody>().velocity=rand_vec;
        }
    }

    void Set_Start(){
        //頂点と順序を入れる
        vertice.AddRange(this.GetComponent<MeshFilter>().mesh.vertices);
        index.AddRange(this.GetComponent<MeshFilter>().mesh.triangles);
        //破壊不能の個数であった場合直す
        brokenum=brokenum>index.Count ? index.Count:brokenum;
        //頂点の連結のグラフ初期化
        for (int i = 0; i < index.Count; i++)
        {
            index_graph.Add(new List<int>(){});
        }
        //グラフを構築
        for (int i = 0; i < index.Count/3; i++)
        {
            index_graph.Add(new List<int>(){});
            //三角形に追加する用の3つの点
            int[] _tri=new int[3];
            for (int j=0;j<3;j++){
                _tri[j]=index[i*3+j];
                //辺を追加
                int[] _edge=new int[]{index[i*3+j],index[i*3+(j+1)%3]};
                //探知用辺
                int[] _edge_ser=new int[]{Mathf.Max(index[i*3+(j+1)%3],index[i*3+j]),Mathf.Min(index[i*3+(j+1)%3],index[i*3+j])};
                //辺のリストに追加
                edge.Add(_edge);
                //現在の辺の番号
                int _edgenum=edge.Count;
                //頂点の連結のグラフ
                index_graph[_edge[0]].Add(_edge[1]);
                
                //繋がってる辺の探知
                if(edge_from.ContainsKey(_edge_ser)){
                    //既に辺がリストにある場合
                    
                    edge_graph.Add(edge_from[_edge_ser]);
                    //逆にも張る
                    foreach (int num in edge_from[_edge_ser])
                    {
                        edge_graph[num].Add(_edgenum);
                    }
                    
                    edge_from[_edge_ser].Add(_edgenum);
                }else{
                    //無い場合
                    edge_graph.Add(new List<int>(){});
                    edge_from.Add(_edge_ser,new List<int>(){_edgenum});
                }
            }
            //三角形に追加
            triangle.Add(_tri);
        }
        

    }
    void Start()
    {
        Set_Start();
        Mesh_Div();
        Create_Obj();

    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown("p")){
            Add_Force();
        }
    }
}
