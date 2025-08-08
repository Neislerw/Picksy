## Relevant Files

- `src/main.ts` - Electron main process, app entry point.
- `src/renderer/App.tsx` - Main React component for the UI.
- `src/renderer/components/BatchSelector.tsx` - Component for folder selection and batch detection.
- `src/renderer/components/PhotoPairViewer.tsx` - Component to display and control photo pair selection.
- `src/renderer/state/saveState.ts` - Module for saving and loading user progress.
- `src/utils/imageBatcher.ts` - Utility for detecting photo batches from metadata.
- `src/types/index.ts` - TypeScript types and interfaces for app data structures.
- `src/renderer/components/__tests__/PhotoPairViewer.test.tsx` - Unit tests for photo pair selection UI.
- `src/utils/__tests__/imageBatcher.test.ts` - Unit tests for batch detection logic.
- `src/renderer/state/__tests__/saveState.test.ts` - Unit tests for save/load state logic.

### Notes

- Unit tests should typically be placed alongside the code files they are testing (e.g., `PhotoPairViewer.tsx` and `PhotoPairViewer.test.tsx` in the same directory).
- Use `npx jest [optional/path/to/test/file]` to run tests. Running without a path executes all tests found by the Jest configuration.

## Tasks

- [x] 1.0 Set up Electron and project structure
  - [x] 1.1 Initialize a new Electron + TypeScript project
  - [x] 1.2 Set up project directory structure (`src/main.ts`, `src/renderer/`, etc.)
  - [x] 1.3 Configure build tools (Webpack, tsconfig, etc.)
  - [x] 1.4 Add cross-platform packaging scripts

  - [ ] 2.0 Implement folder selection and batch detection logic
  - [x] 2.1 Create a UI component for folder selection
  - [x] 2.2 Implement logic to scan selected folder for image files
  - [x] 2.3 Extract metadata (timestamps) from images
  - [x] 2.4 Group images into batches based on time proximity
  - [x] 2.5 Write unit tests for batch detection logic

  - [ ] 3.0 Build the photo pair viewing and selection UI with keyboard controls
  - [x] 3.1 Design and implement the PhotoPairViewer component
  - [x] 3.2 Display two photos from a batch at a time
  - [x] 3.3 Implement keyboard controls (arrow keys, enter, space, z) for selection
  - [x] 3.4 Handle user input to select one, both, or neither photo
  - [x] 3.5 Progress to next pair or batch as appropriate
  - [ ] 3.6 Write unit tests for the UI and input handling

- [x] 4.0 Implement save state and resume functionality
  - [x] 4.1 Design a save state data structure (JSON or similar)
  - [x] 4.2 Implement logic to save user progress to disk
  - [x] 4.3 Implement logic to load and resume from a save state
  - [x] 4.4 Ensure processed photos are skipped on reload
  - [x] 4.5 Write unit tests for save/load logic

- [x] 5.0 UI/UX improvements and visual enhancements
  - [x] 5.1 Fix batch display to show "Batch X/Y" format instead of "Batch batch_1"
  - [x] 5.2 Replace photo count text with progress bar for batch completion
  - [x] 5.3 Remove control explanations from top of screen
  - [x] 5.4 Maximize photo display area by optimizing layout
  - [x] 5.5 Remove bottom buttons from photo viewer
  - [x] 5.6 Add Picksy logo to the start screen
  - [x] 5.7 Add Picksy icon as the app icon for taskbar
  - [x] 5.8 Implement dark theme using dark gray and black colors

- [ ] 6.0 Optimize performance and ensure local-only processing
  - [ ] 6.1 Profile and optimize image loading and batch detection
  - [ ] 6.2 Ensure all processing is done locally (no network calls)
  - [ ] 6.3 Test app responsiveness with large folders

- [ ] 7.0 Write unit tests for core modules
  - [ ] 7.1 Write tests for imageBatcher utility
  - [ ] 7.2 Write tests for saveState module
  - [ ] 7.3 Write tests for UI components

- [ ] 8.0 Add Thumbnail Strip Culling Mode (single-row thumbnails with keyboard-driven delete)
  - [x] 8.1 Start screen integration
    - [x] Add mode selector to choose between Tournament Mode and Thumbnail Strip Mode after folder selection
    - [x] Wire navigation in `src/renderer/App.tsx` to launch the chosen mode
  - [ ] 8.2 Implement `ThumbnailStripCuller` component
    - [x] Render one long horizontal row of large thumbnails with virtualization/lazy loading
    - [x] Keyboard controls: Left/Right to move selection; Space to mark/move selected photo to `_delete` folder; Z to undo last move
    - [x] Visual highlight for the currently selected thumbnail; show filename and index (e.g., 123/2048)
  - [ ] 8.3 Safe delete via main process
    - [x] Create `_delete` folder inside the selected directory if missing
    - [x] Move files (not copy) via IPC to `src/main.ts`; handle name collisions (append suffix) and errors
  - [ ] 8.4 Performance
    - [x] Prefetch adjacent thumbnails; avoid loading full-resolution images; test with very large folders
  - [ ] 8.5 Persist and resume (mode-specific)
    - [ ] Save last selected index and deleted list to skip already moved files on reload
    - [x] Prompt on entering Thumbnail mode if save state exists: offer to skip Tournament-mode processed photos
  - [ ] 8.6 Tests
    - [ ] Unit tests for keyboard handling and selection in `ThumbnailStripCuller`
    - [ ] IPC/file move tests with mocked `fs` to verify `_delete` behavior and collision handling