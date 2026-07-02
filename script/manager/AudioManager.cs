using Godot;

public partial class AudioManager : Node
{
	public static AudioManager Instance { get; private set; }

	[Export] private float sfxMinimumDistance;
	[Export] private float decreaseVolumeFrequency;
	[Export] private AudioStreamPlayer[] sfx;
	[Export] private AudioStreamPlayer[] bgm;

	public bool playSFX = true;
	public bool playBGM = true;
	private int bgmIndex;

	public override void _Ready()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			if (Instance != this)
			{
				QueueFree();
				return;
			}
		}
	}

	public override void _Process(double delta)
	{
		if (!playBGM)
			StopBGM();
		else
		{
			if (bgm != null && bgmIndex < bgm.Length && !bgm[bgmIndex].Playing)
				PlayBGM(bgmIndex);
		}
	}

	public void PlaySFX(int sfxIndex, Node2D source = null)
	{
		if (!playSFX)
			return;

		if (sfx != null && sfxIndex < sfx.Length)
		{
			sfx[sfxIndex].Play();
			GD.Print("Playing SFX with index: " + sfxIndex);
		}
	}

	public void StopSFX(int index)
	{
		if (sfx != null && index < sfx.Length)
			sfx[index].Stop();
	}

	public async void StopSFXWithTime(int index)
	{
		if (sfx != null && index < sfx.Length)
			await DecreaseVolume(sfx[index]);
	}

	public void PlayBGM(int bgmIndex)
	{
		playBGM = true;
		this.bgmIndex = bgmIndex;
		StopBGM();
		if (bgm != null && bgmIndex < bgm.Length)
		{
			bgm[bgmIndex].Play();
			GD.Print("Playing BGM with index: " + bgmIndex);
		}
	}

	private void StopBGM()
	{
		if (bgm == null) return;
		for (int i = 0; i < bgm.Length; i++)
		{
			bgm[i].Stop();
		}
	}

	public async void StopBGMWithTime(int index)
	{
		if (bgm != null && index < bgm.Length)
			await DecreaseVolume(bgm[index]);
	}

	private async System.Threading.Tasks.Task DecreaseVolume(AudioStreamPlayer audio)
	{
		float defaultVolume = audio.VolumeDb;

		while (audio.VolumeDb > 0.1f)
		{
			audio.VolumeDb -= audio.VolumeDb * 0.2f;
			await ToSignal(GetTree().CreateTimer(decreaseVolumeFrequency), Timer.SignalName.Timeout);

			if (audio.VolumeDb <= 0.1f)
			{
				audio.Stop();
				audio.VolumeDb = defaultVolume;
				break;
			}
		}
	}
}
