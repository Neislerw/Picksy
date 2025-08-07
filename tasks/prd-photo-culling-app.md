# PRD: Photo Culling App

## Introduction/Overview
The Photo Culling App is designed to help users efficiently manage and reduce batches of personal photos taken in quick succession. It identifies photo batches based on metadata and allows users to select the best photos from each batch using keyboard controls.

## Goals
- Develop a cross-platform desktop application using TypeScript and Electron.
- Ensure all processing is done locally to maintain user privacy.
- Provide a responsive and intuitive user interface with keyboard controls.

## User Stories
- As a user, I want to select a folder and have the app automatically identify batches of photos.
- As a user, I want to use keyboard controls to quickly select or discard photos from a batch.
- As a user, I want the app to remember my progress and allow me to resume from where I left off.

## Functional Requirements
1. The app must allow users to select a folder containing images.
2. The app must identify batches of photos based on metadata (e.g., timestamps).
3. The app must display two photos from a batch and allow users to select one, both, or neither using keyboard controls (arrow keys, enter, space, q, w, e, z).
4. The app must save the user's progress and allow resuming from a saved state.
5. The app must run locally without requiring an internet connection.

## Non-Goals (Out of Scope)
- The app will not upload or process images on a server.
- The app will not include advanced image editing features.

## Design Considerations (Optional)
- Consider a minimalistic UI design to keep the focus on the photos.
- Ensure the app is responsive and performs well with large image sets.

## Technical Considerations (Optional)
- Use Electron for cross-platform compatibility.
- Optimize image processing for performance.

## Success Metrics
- User satisfaction with the app's performance and ease of use.
- Positive feedback on the app's ability to manage and cull photo batches efficiently.

## Open Questions
- Are there any specific design elements or themes you want to incorporate?
- Do you have any additional features in mind that are not covered here?