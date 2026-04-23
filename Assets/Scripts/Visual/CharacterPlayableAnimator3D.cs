using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[RequireComponent(typeof(Animator))]
public class CharacterPlayableAnimator3D : MonoBehaviour
{
    [SerializeField] string[] leftArmKeys = { "leftarm", "arm_l", "l_arm", "upperarm_l" };
    [SerializeField] string[] rightArmKeys = { "rightarm", "arm_r", "r_arm", "upperarm_r" };
    [SerializeField] string[] leftLegKeys = { "leftleg", "leg_l", "l_leg", "upleg_l" };
    [SerializeField] string[] rightLegKeys = { "rightleg", "leg_r", "r_leg", "upleg_r" };

    Animator _animator;
    PlayableGraph _graph;
    AnimationMixerPlayable _mixer;
    AnimationClipPlayable _idlePlayable;
    AnimationClipPlayable _runPlayable;
    AnimationClipPlayable _attackPlayable;
    bool _initialized;
    double _idleLength = 1.0;
    double _runLength = 1.0;

    float _moveAmount;
    float _attackWeight;
    float _attackTimer;
    const float AttackDuration = 0.22f;

    string _leftArmPath;
    string _rightArmPath;
    string _leftForeArmPath;
    string _rightForeArmPath;
    string _leftLegPath;
    string _rightLegPath;
    string _spinePath;
    string _rootPath = string.Empty;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _animator.applyRootMotion = false;
        CacheBonePaths();
        BuildGraph();
    }

    void Update()
    {
        if (!_initialized || !_graph.IsValid() || !_mixer.IsValid())
            return;

        ForceLoop(ref _idlePlayable, _idleLength);
        ForceLoop(ref _runPlayable, _runLength);

        _moveAmount = Mathf.MoveTowards(_moveAmount, 0f, Time.deltaTime * 3.2f);
        if (_mixer.GetInputCount() >= 2)
        {
            _mixer.SetInputWeight(0, Mathf.Clamp01(1f - _moveAmount));
            _mixer.SetInputWeight(1, Mathf.Clamp01(_moveAmount));
        }

        if (_attackWeight > 0f)
        {
            _attackTimer += Time.deltaTime;
            _attackWeight = Mathf.Clamp01(1f - (_attackTimer / AttackDuration));
            if (_attackWeight <= 0f)
            {
                if (_attackPlayable.IsValid())
                {
                    _attackPlayable.SetTime(0.0);
                    _attackPlayable.SetSpeed(0.0);
                }
            }
        }

        if (_mixer.GetInputCount() >= 3)
            _mixer.SetInputWeight(2, _attackWeight);
    }

    public void SetMoveAmount(float amount01)
    {
        if (!_initialized || !_graph.IsValid() || !_mixer.IsValid()) return;
        _moveAmount = Mathf.Clamp01(Mathf.Max(_moveAmount, amount01));
    }

    public void TriggerAttack()
    {
        if (!_initialized || !_graph.IsValid() || !_mixer.IsValid() || !_attackPlayable.IsValid()) return;
        _attackTimer = 0f;
        _attackWeight = 1f;
        _attackPlayable.SetTime(0.0);
        _attackPlayable.SetSpeed(1.0);
    }

    void CacheBonePaths()
    {
        var all = GetComponentsInChildren<Transform>(true);
        Transform la = GetHumanoidBone(HumanBodyBones.LeftUpperArm) ?? FindBone(all, leftArmKeys);
        Transform ra = GetHumanoidBone(HumanBodyBones.RightUpperArm) ?? FindBone(all, rightArmKeys);
        Transform lfa = GetHumanoidBone(HumanBodyBones.LeftLowerArm);
        Transform rfa = GetHumanoidBone(HumanBodyBones.RightLowerArm);
        Transform ll = GetHumanoidBone(HumanBodyBones.LeftUpperLeg) ?? FindBone(all, leftLegKeys);
        Transform rl = GetHumanoidBone(HumanBodyBones.RightUpperLeg) ?? FindBone(all, rightLegKeys);
        Transform sp = GetHumanoidBone(HumanBodyBones.Spine);

        _leftArmPath = la != null ? GetPath(la) : null;
        _rightArmPath = ra != null ? GetPath(ra) : null;
        _leftForeArmPath = lfa != null ? GetPath(lfa) : null;
        _rightForeArmPath = rfa != null ? GetPath(rfa) : null;
        _leftLegPath = ll != null ? GetPath(ll) : null;
        _rightLegPath = rl != null ? GetPath(rl) : null;
        _spinePath = sp != null ? GetPath(sp) : null;
    }

    Transform GetHumanoidBone(HumanBodyBones bone)
    {
        if (_animator == null || !_animator.isHuman)
            return null;
        return _animator.GetBoneTransform(bone);
    }

    void BuildGraph()
    {
        if (_animator == null) return;

        var idle = BuildIdleClip();
        var run = BuildRunClip();
        var attack = BuildAttackClip();
        _idleLength = Mathf.Max(0.1f, idle.length);
        _runLength = Mathf.Max(0.1f, run.length);

        _graph = PlayableGraph.Create($"{name}_CharacterPlayableAnimator3D");
        _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        var output = AnimationPlayableOutput.Create(_graph, "Animation", _animator);
        _mixer = AnimationMixerPlayable.Create(_graph, 3, true);
        output.SetSourcePlayable(_mixer);

        _idlePlayable = AnimationClipPlayable.Create(_graph, idle);
        _runPlayable = AnimationClipPlayable.Create(_graph, run);
        _attackPlayable = AnimationClipPlayable.Create(_graph, attack);
        _idlePlayable.SetApplyFootIK(false);
        _runPlayable.SetApplyFootIK(false);
        _attackPlayable.SetApplyFootIK(false);
        _attackPlayable.SetSpeed(0.0);

        _graph.Connect(_idlePlayable, 0, _mixer, 0);
        _graph.Connect(_runPlayable, 0, _mixer, 1);
        _graph.Connect(_attackPlayable, 0, _mixer, 2);

        _mixer.SetInputWeight(0, 1f);
        _mixer.SetInputWeight(1, 0f);
        _mixer.SetInputWeight(2, 0f);

        _graph.Play();
        _initialized = _graph.IsValid() && _mixer.IsValid() && _attackPlayable.IsValid();
    }

    AnimationClip BuildIdleClip()
    {
        var clip = new AnimationClip { name = "Idle_ProceduralPlayable" };
        AddSwingCurve(clip, _leftArmPath, 4f, 0f);
        AddSwingCurve(clip, _rightArmPath, -4f, 0f);
        AddSwingCurve(clip, _leftLegPath, -2f, 0f);
        AddSwingCurve(clip, _rightLegPath, 2f, 0f);
        AddBodyBobCurve(clip, _rootPath, 0.015f);
        clip.wrapMode = WrapMode.Loop;
        return clip;
    }

    AnimationClip BuildRunClip()
    {
        var clip = new AnimationClip { name = "Run_ProceduralPlayable" };
        AddSwingCurve(clip, _leftArmPath, 32f, 0f);
        AddSwingCurve(clip, _rightArmPath, -32f, 0f);
        AddSwingCurve(clip, _leftLegPath, -28f, 0f);
        AddSwingCurve(clip, _rightLegPath, 28f, 0f);
        // Always-visible run motion, even when bone names don't match.
        AddBodyBobCurve(clip, _rootPath, 0.075f);
        AddBodyTiltCurve(clip, _rootPath, 8f);
        clip.wrapMode = WrapMode.Loop;
        return clip;
    }

    AnimationClip BuildAttackClip()
    {
        var clip = new AnimationClip { name = "Attack_ProceduralPlayable" };
        if (!string.IsNullOrEmpty(_rightArmPath))
        {
            Keyframe[] keys =
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.04f, -34f),
                new Keyframe(0.12f, 16f),
                new Keyframe(0.22f, 0f)
            };
            clip.SetCurve(_rightArmPath, typeof(Transform), "localEulerAnglesRaw.x", new AnimationCurve(keys));
        }
        if (!string.IsNullOrEmpty(_rightForeArmPath))
        {
            Keyframe[] keys =
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.05f, -28f),
                new Keyframe(0.12f, 22f),
                new Keyframe(0.22f, 0f)
            };
            clip.SetCurve(_rightForeArmPath, typeof(Transform), "localEulerAnglesRaw.x", new AnimationCurve(keys));
        }
        if (!string.IsNullOrEmpty(_leftArmPath))
        {
            Keyframe[] keys =
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.04f, 12f),
                new Keyframe(0.22f, 0f)
            };
            clip.SetCurve(_leftArmPath, typeof(Transform), "localEulerAnglesRaw.x", new AnimationCurve(keys));
        }
        if (!string.IsNullOrEmpty(_spinePath))
        {
            Keyframe[] keysX =
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.05f, -8f),
                new Keyframe(0.22f, 0f)
            };
            Keyframe[] keysY =
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.05f, 10f),
                new Keyframe(0.22f, 0f)
            };
            clip.SetCurve(_spinePath, typeof(Transform), "localEulerAnglesRaw.x", new AnimationCurve(keysX));
            clip.SetCurve(_spinePath, typeof(Transform), "localEulerAnglesRaw.y", new AnimationCurve(keysY));
        }
        // Always-visible attack recoil for any rig.
        AddAttackBodyKick(clip, _rootPath);
        clip.wrapMode = WrapMode.Once;
        return clip;
    }

    void AddSwingCurve(AnimationClip clip, string path, float amplitude, float phase)
    {
        if (string.IsNullOrEmpty(path)) return;
        Keyframe[] keys =
        {
            new Keyframe(0f, Mathf.Sin(phase) * amplitude),
            new Keyframe(0.25f, Mathf.Sin(phase + Mathf.PI * 0.5f) * amplitude),
            new Keyframe(0.5f, Mathf.Sin(phase + Mathf.PI) * amplitude),
            new Keyframe(0.75f, Mathf.Sin(phase + Mathf.PI * 1.5f) * amplitude),
            new Keyframe(1f, Mathf.Sin(phase + Mathf.PI * 2f) * amplitude)
        };
        clip.SetCurve(path, typeof(Transform), "localEulerAnglesRaw.x", new AnimationCurve(keys));
    }

    void AddBodyBobCurve(AnimationClip clip, string path, float amplitude)
    {
        if (string.IsNullOrEmpty(path) && path != string.Empty) return;
        Keyframe[] keys =
        {
            new Keyframe(0f, 0f),
            new Keyframe(0.25f, amplitude),
            new Keyframe(0.5f, 0f),
            new Keyframe(0.75f, -amplitude * 0.7f),
            new Keyframe(1f, 0f)
        };
        clip.SetCurve(path, typeof(Transform), "localPosition.y", new AnimationCurve(keys));
    }

    void AddBodyTiltCurve(AnimationClip clip, string path, float angle)
    {
        if (string.IsNullOrEmpty(path) && path != string.Empty) return;
        Keyframe[] keys =
        {
            new Keyframe(0f, 0f),
            new Keyframe(0.25f, angle),
            new Keyframe(0.5f, 0f),
            new Keyframe(0.75f, -angle),
            new Keyframe(1f, 0f)
        };
        clip.SetCurve(path, typeof(Transform), "localEulerAnglesRaw.z", new AnimationCurve(keys));
    }

    void AddAttackBodyKick(AnimationClip clip, string path)
    {
        if (string.IsNullOrEmpty(path) && path != string.Empty) return;
        Keyframe[] posZ =
        {
            new Keyframe(0f, 0f),
            new Keyframe(0.05f, -0.07f),
            new Keyframe(0.12f, 0.04f),
            new Keyframe(0.22f, 0f)
        };
        Keyframe[] rotX =
        {
            new Keyframe(0f, 0f),
            new Keyframe(0.05f, -14f),
            new Keyframe(0.12f, 8f),
            new Keyframe(0.22f, 0f)
        };
        clip.SetCurve(path, typeof(Transform), "localPosition.z", new AnimationCurve(posZ));
        clip.SetCurve(path, typeof(Transform), "localEulerAnglesRaw.x", new AnimationCurve(rotX));
    }

    static Transform FindBone(Transform[] bones, string[] keys)
    {
        for (int i = 0; i < keys.Length; i++)
        {
            string key = keys[i];
            for (int j = 0; j < bones.Length; j++)
            {
                var t = bones[j];
                if (t == null) continue;
                if (t.name.ToLowerInvariant().Contains(key))
                    return t;
            }
        }
        return null;
    }

    string GetPath(Transform target)
    {
        if (target == null) return string.Empty;
        if (target == transform) return string.Empty;
        string path = target.name;
        var current = target.parent;
        while (current != null && current != transform)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }

    void ForceLoop(ref AnimationClipPlayable playable, double length)
    {
        if (!playable.IsValid()) return;
        if (length <= 0.0001) return;
        double t = playable.GetTime();
        if (t >= length)
            playable.SetTime(t % length);
    }

    void OnDestroy()
    {
        _initialized = false;
        if (_graph.IsValid())
            _graph.Destroy();
    }
}
