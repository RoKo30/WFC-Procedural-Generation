using System;
using System.Diagnostics;
using ProceduralGen.Wfc.Core;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ProceduralGen.Wfc
{
    // Scene component that runs overlapping WFC on an input image and shows the result on its SpriteRenderer
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class WfcGenerator : MonoBehaviour
    {
        [Tooltip("Small example image. Needs Read/Write enabled, Point filtering and no compression.")]
        [SerializeField] private Texture2D inputImage;

        [SerializeField] private WfcSettings settings = new WfcSettings();

        [Header("Display")]
        [SerializeField, Min(0.01f)] private float pixelsPerUnit = 8f;
        [Tooltip("While animating, colour undecided cells with the average of their remaining options.")]
        [SerializeField] private bool showSuperposition = true;
        [SerializeField] private Color undecidedColor = new Color(0.12f, 0.12f, 0.14f, 1f);

        [Header("Playback")]
        [SerializeField] private bool generateOnStart = true;
        [Tooltip("Animate generation step by step instead of producing the result in one go.")]
        [SerializeField] private bool animate = true;
        [Tooltip("Cells collapsed per frame while animating.")]
        [SerializeField, Min(1)] private int stepsPerFrame = 4;

        private SpriteRenderer _spriteRenderer;
        private WfcTextureRenderer _renderer;
        private Sprite _sprite;

        public Texture2D InputImage
        {
            get => inputImage;
            set => inputImage = value;
        }

        public WfcSettings Settings => settings;
        public bool Animate => animate;

        public PatternSet Patterns { get; private set; }
        public WfcGeneration Generation { get; private set; }
        public Texture2D OutputTexture => _renderer?.Texture;
        public bool IsAnimating { get; private set; }
        public string StatusMessage { get; private set; } = "Not generated yet.";
        public double LastBuildMilliseconds { get; private set; }
        public double LastSolveMilliseconds { get; private set; }

        public event Action<WfcGenerator> Finished;

        private void OnValidate() => settings.Validate();

        private void Start()
        {
            if (!generateOnStart) return;
            if (animate) BeginAnimated();
            else Generate();
        }

        private void Update()
        {
            if (IsAnimating) Tick();
        }

        private void OnDestroy() => ReleaseOutput();

        public bool Generate()
        {
            if (!Prepare()) return false;

            var timer = Stopwatch.StartNew();
            Generation.RunToCompletion();
            LastSolveMilliseconds = timer.Elapsed.TotalMilliseconds;

            Present();
            Finished?.Invoke(this);
            return Generation.State == GenerationState.Succeeded;
        }

        public bool BeginAnimated()
        {
            if (!Prepare()) return false;
            LastSolveMilliseconds = 0;
            IsAnimating = true;
            Present();
            return true;
        }

        public void Tick() => Advance(stepsPerFrame);

        public void StepOnce()
        {
            if (Generation == null || Generation.State != GenerationState.Running)
            {
                if (Prepare()) Present();
                return;
            }
            Advance(1);
        }

        public void Stop() => IsAnimating = false;

        private void Advance(int steps)
        {
            if (Generation == null) return;

            var timer = Stopwatch.StartNew();
            for (int i = 0; i < steps; i++)
            {
                if (Generation.Step() == GenerationState.Running) continue;
                IsAnimating = false;
                break;
            }
            LastSolveMilliseconds += timer.Elapsed.TotalMilliseconds;

            Present();
            if (Generation.State != GenerationState.Running) Finished?.Invoke(this);
        }

        private bool Prepare()
        {
            IsAnimating = false;
            if (inputImage == null)
            {
                StatusMessage = "Assign an input image.";
                return false;
            }

            try
            {
                settings.Validate();
                if (settings.randomizeSeed)
                    settings.seed = new System.Random().Next();

                var timer = Stopwatch.StartNew();
                ImageData image = TextureConversion.ToImageData(inputImage);
                Patterns = PatternExtractor.Extract(image, settings.ToExtractionOptions());
                AdjacencyRules rules = AdjacencyRules.Build(Patterns, settings.overlap);
                Generation = new WfcGeneration(Patterns, rules, settings.ToGenerationOptions());
                LastBuildMilliseconds = timer.Elapsed.TotalMilliseconds;
            }
            catch (ArgumentException e)
            {
                Generation = null;
                StatusMessage = e.Message;
                Debug.LogError($"[WFC] {e.Message}", this);
                return false;
            }

            CreateOutput();
            return true;
        }

        private void CreateOutput()
        {
            ReleaseOutput();
            _renderer = new WfcTextureRenderer(Patterns, Generation.Grid);

            Texture2D texture = _renderer.Texture;
            _sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                                    new Vector2(0.5f, 0.5f), pixelsPerUnit);
            _sprite.name = "WFC Output";
            _sprite.hideFlags = HideFlags.DontSave;

            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            _spriteRenderer.sprite = _sprite;
        }

        private void Present()
        {
            bool inProgress = Generation.State == GenerationState.Running;
            _renderer.Render(Generation.Solver, showSuperposition && inProgress, undecidedColor);
            StatusMessage = DescribeState();
        }

        private string DescribeState()
        {
            WfcGeneration g = Generation;
            OutputGrid grid = g.Grid;
            string state;
            switch (g.State)
            {
                case GenerationState.Succeeded: state = "Done"; break;
                case GenerationState.Failed: state = $"Failed: contradiction in all {g.Options.MaxAttempts} attempts"; break;
                default: state = $"Running ({g.Solver.UndecidedCount} cells undecided)"; break;
            }

            return $"{state}\n" +
                   $"Attempt {g.Attempt}/{g.Options.MaxAttempts} · seed {g.Options.Seed}\n" +
                   $"{Patterns.Count} unique patterns · {Patterns.Palette.Length} colours\n" +
                   $"Grid {grid.CellsX}×{grid.CellsY} cells (stride {grid.Stride}) → {grid.ImageWidth}×{grid.ImageHeight} px\n" +
                   $"Build {LastBuildMilliseconds:0} ms · solve {LastSolveMilliseconds:0} ms";
        }

        private void ReleaseOutput()
        {
            if (_sprite != null)
            {
                if (_spriteRenderer != null && _spriteRenderer.sprite == _sprite) _spriteRenderer.sprite = null;
                if (Application.isPlaying) Destroy(_sprite);
                else DestroyImmediate(_sprite);
                _sprite = null;
            }
            _renderer?.Dispose();
            _renderer = null;
        }
    }
}
