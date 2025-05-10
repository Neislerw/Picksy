# Picksy

![Picksy Logo](Resources/logo.png)

Picksy is a Windows Application that helps you slim down your photo albums by grouping images in to batches and letting you systematically select favorites.  Non-Selected images are moved to a "_delete" folder where you can delete permenantly or archive.

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