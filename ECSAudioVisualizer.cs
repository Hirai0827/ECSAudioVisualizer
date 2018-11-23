using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
[RequireComponent(typeof(Camera))]
public sealed class MyAudioCubeSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        var chunks = EntityManager.CreateArchetypeChunkArray(query, Allocator.TempJob);
        var positionType = GetArchetypeChunkComponentType<Position>(false);
        var scaleType = GetArchetypeChunkComponentType<Scale>(false);
        var materialType = GetArchetypeChunkSharedComponentType<MeshInstanceRenderer>();
        var time = Time.realtimeSinceStartup;
        for (int chunkIndex = 0, length = chunks.Length; chunkIndex < length; chunkIndex++)
        {
            var chunk = chunks[chunkIndex];
            var positions = chunk.GetNativeArray(positionType);
            var scales = chunk.GetNativeArray(scaleType);
            
            for (int i = 0, chunkCount = chunk.Count; i < chunkCount; i++)
            {
                var position = positions[i];
                var scale = scales[i];
                // position.Value.y = math.sin(time + 0.2f * (position.Value.x)) * math.sin(time + 0.2f * (position.Value.z));
                //position.Value.y = Mathf.Pow(ECSAudioVisualizer.audioData[((int)(position.Value.x / 4) + (int)(position.Value.z / 4)) % 4],2);
                scale.Value.y += Mathf.Pow(ECSAudioVisualizer.audioData[((int)(position.Value.x / 1) + (int)(position.Value.z / 1)) % 32], 2);
                scale.Value.y /= 2;
                scales[i] = scale;
                //positions[i] = position;
            }
        }
        chunks.Dispose();
    }
    private readonly EntityArchetypeQuery query = new EntityArchetypeQuery
    {
        Any = System.Array.Empty<ComponentType>(),
        None = System.Array.Empty<ComponentType>(),
        All = new ComponentType[] { ComponentType.Create<Position>(), ComponentType.Create<Scale>(), ComponentType.Create<MeshInstanceRenderer>() }
    };
}
public class ECSAudioVisualizer : MonoBehaviour
{

    public Material[] materials;
    public AudioSource targetAudio;
    public static float[] audioData;
    public int divideLength;
    // Start is called before the first frame update
    void Start()
    {
        InitializeWorld();
        CreateCubeForECS();
        audioData = new float[1024 / divideLength];
    }

    private void Update()
    {
        float[] spectrum = new float[1024];
        targetAudio.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
        for(int i = 0; i < 1024 / divideLength; i++)
        {
            audioData[i] = 0;
            for(int j = i * divideLength; j < (i + 1) * divideLength; j++)
            {
                audioData[i] += spectrum[j];
            }
            audioData[i] /= divideLength;
            audioData[i] = Mathf.Log10(audioData[i]) + 7;
            Debug.Log(i + ":" + audioData[i]);
        }
    }


    // Update is called once per frame
    private void OnDisable()
    {
        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(null);
        _world?.Dispose();
    }
    private void InitializeWorld()
    {
        _world = World.Active = new World("MyWorld");
        _world.CreateManager(typeof(EntityManager));
        _world.CreateManager(typeof(EndFrameTransformSystem));
        _world.CreateManager(typeof(EndFrameBarrier));
        _world.CreateManager<MeshInstanceRendererSystem>().ActiveCamera = GetComponent<Camera>();
        _world.CreateManager(typeof(RenderingSystemBootstrap));
        _world.CreateManager(typeof(MyAudioCubeSystem));
        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(_world);
    }
    private World _world;
    void CreateCube()
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = Vector3.zero;
        cube.transform.rotation = Quaternion.identity;
        cube.transform.localScale = Vector3.one;
    }
    void CreateCubeForECS()
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cube.transform.position = Vector3.zero;
        cube.transform.rotation = Quaternion.identity;
        cube.transform.localScale = Vector3.one;
        var manager = _world?.GetExistingManager<EntityManager>();
        if (manager != null)
        {
            var archetype = manager.CreateArchetype(ComponentType.Create<Position>(),ComponentType.Create<Scale>(),
                ComponentType.Create<MeshInstanceRenderer>());
            for (int x = 0; x < 50; x++)
            {
                for (int z = 0; z < 50; z++)
                {
                    var entity = manager.CreateEntity(archetype);
                    manager.SetComponentData(entity, new Position() { Value = new float3(x, 0, z) });
                    manager.SetComponentData(entity, new Scale() { Value = new float3(0.9f, 1, 0.9f) });
                    manager.SetSharedComponentData(entity, new MeshInstanceRenderer()
                    {
                        mesh = cube.GetComponent<MeshFilter>().sharedMesh,
                        material = materials[(x + z) % materials.Length],
                        subMesh = 0,
                        castShadows = UnityEngine.Rendering.ShadowCastingMode.Off,
                        receiveShadows = false
                    });
                }
            }

        }
        Destroy(cube);
    }

}
