# PicPick

![PicPick Logo](Resources/logo.png)

PicPick is an open-source Windows application that helps you organize photos by grouping them into batches based on timestamped filenames and selecting favorites through a tournament-style comparison. Non-selected photos can be moved to a "_delete" folder or kept.

## Features
- **Photo Grouping**: Groups images with filenames like `YYYYMMDD_HHMMSS_XXX.jpg` (e.g., `20250122_171223_003.jpg`) taken within 20 seconds of each other (minimum 4 photos per batch).
- **Tournament-Style Selection**:
  - Compare two photos side-by-side.
  - Select a favorite by clicking or using Left/Right arrow keys.
  - Keep both photos with the Up arrow.
  - Undo the last action with the Down arrow.
  - Keep all remaining photos with the Spacebar.
- **Post-Selection Actions**:
  - View thumbnails of non-selected (losing) photos.
  - Move losers to a "_delete" folder or cancel to keep all photos.
- **Error Handling**: Robust handling of invalid files, duplicate filenames, and file access issues.

## Prerequisites
- [.NET SDK 9.0 or later](https://dotnet.microsoft.com/download/dotnet/9.0)
- Windows operating system (for Windows Forms)
- [Visual Studio Code](https://code.visualstudio.com/) or [Cursor](https://www.cursor.com/) with the C# extension

## Setup
1. Clone the repository:
   ```bash
   git clone https://github.com/<your-username>/PicPick.git
   cd PicPick