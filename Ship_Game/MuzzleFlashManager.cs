using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	internal class MuzzleFlashManager
	{
		public static UniverseScreen universeScreen;

		public static Texture2D FlashTexture;

		public static Model flashModel;

		public static BatchRemovalCollection<MuzzleFlash> FlashList;

		static MuzzleFlashManager()
		{
			MuzzleFlashManager.FlashList = new BatchRemovalCollection<MuzzleFlash>();
		}

		public MuzzleFlashManager()
		{
		}

		public static void Update(float elapsedTime)
		{
			lock (GlobalStats.ExplosionLocker)
			{
				for (int i = 0; i < MuzzleFlashManager.FlashList.Count; i++)
				{
					if (MuzzleFlashManager.FlashList[i] != null)
					{
						MuzzleFlash item = MuzzleFlashManager.FlashList[i];
						item.timer = item.timer - elapsedTime;
						MuzzleFlash muzzleFlash = MuzzleFlashManager.FlashList[i];
						muzzleFlash.scale = muzzleFlash.scale * 2f;
						if (MuzzleFlashManager.FlashList[i].scale > 6f)
						{
							MuzzleFlashManager.FlashList[i].scale = 6f;
						}
						if (MuzzleFlashManager.FlashList[i].timer <= 0f)
						{
							MuzzleFlashManager.FlashList.QueuePendingRemoval(MuzzleFlashManager.FlashList[i]);
						}
					}
				}
			}
		}
	}
}