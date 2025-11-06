# Feature #4 Manual Testing Guide

## Test Scenario: Track/Clip UI Basic Functionality

### Prerequisites
- Application launched via `dotnet run --project src/StudioSoundPro.UI/StudioSoundPro.UI.csproj`
- Window opened successfully

---

## Test Cases

### TC1: Application Launch
**Expected:**
- [✅] Window opens with title "StudioSoundPro"
- [✅] Transport controls visible at top
- [✅] TabControl with two tabs: "Timeline" and "Audio Test"
- [✅] Default tab is "Timeline"

### TC2: Timeline View Initial State
**Expected:**
- [✅] Toolbar visible with buttons: "+ Audio Track", "+ Track", track count, "Clear All"
- [✅] Track count shows "Tracks: 0"
- [✅] "Clear All" button is disabled (no tracks)
- [✅] Empty track list area below toolbar

### TC3: Add Audio Track
**Steps:**
1. Click "+ Audio Track" button

**Expected:**
- [✅] New track appears in list
- [✅] Track count updates to "Tracks: 1"
- [✅] Track has default name (e.g., "Track 1")
- [✅] Track color is blue (#2196F3)
- [✅] Track header shows:
  - Editable name textbox
  - M/S/R buttons (gray background, not active)
  - Volume slider with "0.0 dB" label
  - Pan slider with "C" label
- [✅] Timeline area on right (dark background, empty)
- [✅] "Clear All" button becomes enabled

### TC4: Add Multiple Tracks
**Steps:**
1. Click "+ Track" button 3 times

**Expected:**
- [✅] Track count shows "Tracks: 4" (1 audio + 3 regular)
- [✅] Each track has different color (cycling through palette)
- [✅] Tracks stacked vertically with borders between them

### TC5: Track Name Editing
**Steps:**
1. Click on track name textbox
2. Type "Lead Vocals"
3. Press Enter or click away

**Expected:**
- [✅] Track name updates to "Lead Vocals"
- [✅] Name persists when clicking away

### TC6: Mute Button
**Steps:**
1. Click "M" button on first track

**Expected:**
- [✅] Button background turns red (#FF5722)
- [✅] Button remains pressed (toggle state)
- [✅] Click again to unmute
- [✅] Button returns to gray background

### TC7: Solo Button
**Steps:**
1. Click "S" button on second track

**Expected:**
- [✅] Button background turns yellow (#FFC107)
- [✅] Button remains pressed
- [✅] Click again to unsolo
- [✅] Button returns to gray

### TC8: Arm Button
**Steps:**
1. Click "R" button on third track

**Expected:**
- [✅] Button background turns red (#F44336)
- [✅] Button remains pressed
- [✅] Click again to disarm
- [✅] Button returns to gray

### TC9: Volume Control
**Steps:**
1. Drag volume slider on first track to the right
2. Observe dB label

**Expected:**
- [✅] Slider moves smoothly
- [✅] dB label updates in real-time
- [✅] Values range from -∞ dB (volume=0) to +6 dB (volume=2)
- [✅] Label shows decimal precision (e.g., "3.5 dB")

### TC10: Pan Control
**Steps:**
1. Drag pan slider on first track to the left
2. Drag to center
3. Drag to the right

**Expected:**
- [✅] Slider moves smoothly
- [✅] Label shows "L" (left), "C" (center), or "R" (right) with percentage
- [✅] Pan ranges from -100% (full left) to +100% (full right)

### TC11: Clear All Tracks
**Steps:**
1. Click "Clear All" button

**Expected:**
- [✅] All tracks removed from view
- [✅] Track count shows "Tracks: 0"
- [✅] "Clear All" button becomes disabled
- [✅] Empty track list area

### TC12: Transport Controls Integration
**Steps:**
1. Add a track
2. Click Play button in transport controls
3. Observe track view

**Expected:**
- [✅] Transport controls respond (Play/Pause/Stop buttons work)
- [✅] Time display updates
- [✅] Track view remains stable (no rendering issues)
- [✅] No playhead visible yet (pending timeline rendering implementation)

### TC13: Audio Test Tab
**Steps:**
1. Click "Audio Test" tab

**Expected:**
- [✅] Tab switches to Audio Test view
- [✅] Existing audio test controls visible
- [✅] Click back to "Timeline" tab
- [✅] Tracks persist (session state maintained)

### TC14: Track Scrolling
**Steps:**
1. Add 10+ tracks
2. Scroll vertically in track list

**Expected:**
- [✅] Vertical scrollbar appears
- [✅] Scrolling is smooth
- [✅] All tracks accessible
- [✅] Track headers remain aligned with timeline areas

### TC15: Timeline Horizontal Scroll
**Steps:**
1. Add a track
2. Scroll horizontally in timeline area (right side of track)

**Expected:**
- [✅] Horizontal scrollbar appears
- [✅] Timeline canvas scrolls (8000px width)
- [✅] No clips visible (placeholder comment only)

---

## Known Limitations (Expected Behavior)

1. **No Clip Rendering:** Timeline canvas is empty. Clips cannot be added or visualized yet.
2. **No Playhead:** Transport playback does not show position cursor on timeline.
3. **No Waveforms:** ClipView waveform placeholder is just a dark rectangle.
4. **No Drag-and-Drop:** Cannot drag clips or audio files.
5. **No Context Menus:** Right-click does not show options.
6. **No Keyboard Shortcuts:** Keyboard input only works for text fields.

---

## Bug Reporting Template

If any test fails, report using this format:

**Test Case:** [TC number and name]  
**Steps to Reproduce:**  
1. [Step 1]  
2. [Step 2]  
...

**Expected Result:** [What should happen]  
**Actual Result:** [What actually happened]  
**Screenshot:** [If applicable]  
**Console Output:** [Any error messages]  

---

## Test Summary

**Date:** [Fill in]  
**Tester:** [Fill in]  
**Build:** Feature #4 Phase 1  

| Test Case | Status | Notes |
|-----------|--------|-------|
| TC1: Application Launch | ⬜ | |
| TC2: Timeline Initial State | ⬜ | |
| TC3: Add Audio Track | ⬜ | |
| TC4: Add Multiple Tracks | ⬜ | |
| TC5: Track Name Editing | ⬜ | |
| TC6: Mute Button | ⬜ | |
| TC7: Solo Button | ⬜ | |
| TC8: Arm Button | ⬜ | |
| TC9: Volume Control | ⬜ | |
| TC10: Pan Control | ⬜ | |
| TC11: Clear All Tracks | ⬜ | |
| TC12: Transport Integration | ⬜ | |
| TC13: Audio Test Tab | ⬜ | |
| TC14: Track Scrolling | ⬜ | |
| TC15: Timeline Scroll | ⬜ | |

**Overall Status:** ⬜ Pass / ⬜ Fail  
**Comments:** [Additional observations]

---

## Next Steps After Testing

1. Document any bugs found in GitHub issues
2. Proceed to Feature #4 Phase 2 (Timeline Rendering) if all tests pass
3. Update completion report with manual testing results
