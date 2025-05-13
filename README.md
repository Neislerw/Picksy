# Picksy

![Picksy Logo](Resources/logo-light.png)

Picksy is a Windows Application that helps you slim down your photo albums by grouping images in to batches and letting you systematically select favorites.  Non-Selected images are moved to a "_delete" folder where you can delete permenantly or archive.

If you are like me, when you take photos of things you take several all at once to try and get a good one. But over time, those extra photos sit in your camera roll taking up space and cluttering your gallery. When you go to show someone a photo from one of those batches, you just kinda guess from the thumbnail which is the "good one" or maybe flip through them.
I always had this problem and never found the time to sit down and sort through them.  Eventually I had over 100GB of photos in my Google Photos account.  I was too afraid to delete the batches because ONE of them IS the best one, and I just didn't know which one it was yet.  So I made this tool to help me systematically review batches of photos and select ones for deletion.

Picksy is FREE, it is Open Source, and it runs entirely locally on your computer (your photos stay private!).  It is also Non-Destructive; Picksy doesn't delete anything, it simply moves your non-selected photos to a subfolder called "_delete" where you can make the final call on whether you want to archive them or delete them forever.

You can 'Save and Quit' and Picksy will keep a savestate file in your folder that handles which pictures have been reviewed and which have not.  You can come back and finish reviewing photos at a later date.  You can even keep adding photos and Picksy will know which ones have been kept previously to save you time!

## Features
- **Photo Grouping**: Groups images with filenames like `YYYYMMDD_HHMMSS_XXX.jpg` (e.g., `20250122_171223_003.jpg`) taken within close proximity of each other.
- **Tournament-Style Selection**:
  - Compare two photos side-by-side
  - Select a favorite by clicking or using Left/Right arrow keys
  - Keep both photos with the Up arrow
  - Undo the last action with the Down arrow
  - Keep all remaining photos with the 'Spacebar'
  - See Full Resolution of Batch by pressing 'W'
  - Delete ALL of a batch with 'Del'
- **Post-Selection Actions**:
  - View thumbnails of non-selected photos.
  - Move non-selected to a "_delete" folder or cancel to keep all photos.

![Picksy Demo](Resources/demo_1.gif)

## Prerequisites
- [.NET SDK 9.0 or later](https://dotnet.microsoft.com/download/dotnet/9.0)
- Windows operating system (for Windows Forms)
- [Visual Studio Code](https://code.visualstudio.com/) or [Cursor](https://www.cursor.com/) with the C# extension

## Setup
1. Clone the repository:
   ```bash
   git clone https://github.com/<your-username>/Picksy.git
   cd Picksy

## License

  Picksy is licensed under the [GNU AGPLv3](LICENSE.md) for open source use, ensuring that any derivative works, including SaaS applications, remain open source.

  For commercial use without AGPLv3 obligations (e.g., proprietary products or SaaS), a commercial license is available. Contact neislerw@gmail.com for details.

## Support Picksy
[Buy me a Coffee](buymeacoffee.com/neislerw) or Share with a friend!
