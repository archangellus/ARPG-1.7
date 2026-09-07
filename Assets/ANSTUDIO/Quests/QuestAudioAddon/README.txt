Quest Audio Add-on
==================

Purpose
-------
Adds start/completion dialogue audio to the existing PLAYER TWO ARPG quest system
without editing Quest.cs, QuestGiver.cs, or GUIDialogue.cs.

Files
-----
QuestAudioDatabase.cs
    ScriptableObject that maps an existing Quest asset to:
    - Start Dialogue Audio
    - Complete Dialogue Audio

GUIDialogueAudioExtensions.cs
    Calls the existing GUIDialogue.ShowAndFill(...) and then plays the optional
    AudioClip using GameAudio.instance.PlayUiEffect(...).

QuestGiverAudio.cs
    Subclass of QuestGiver. It keeps the original quest logic but looks up the
    corresponding audio clips in QuestAudioDatabase.

Unity setup
-----------
1. Copy the three .cs files into your project's Assets folder, preferably:
   Assets/Scripts/QuestAudio/

2. Wait for Unity to compile.

3. Create the database:
   Assets -> Create -> PLAYER TWO -> ARPG Project -> Quest -> Quest Audio Database

4. In the database's Entries array:
   - assign the existing Quest asset
   - assign Start Dialogue Audio
   - assign Complete Dialogue Audio

5. On each NPC that currently uses QuestGiver:
   - replace QuestGiver with QuestGiverAudio
   - restore/copy its Quest Giver Name, Quests, and On State Change values
   - assign the Quest Audio Database

6. Keep your original GUIDialogue component and package scripts unchanged.

Notes
-----
- Audio is optional. If a quest has no matching database entry or a clip is empty,
  dialogue works exactly as before.
- This intentionally mirrors the pasted implementation's use of
  GameAudio.instance.PlayUiEffect(AudioClip).
- Because QuestGiverAudio is a subclass, it must be used in place of QuestGiver
  on quest-giver NPCs that need quest dialogue audio.
