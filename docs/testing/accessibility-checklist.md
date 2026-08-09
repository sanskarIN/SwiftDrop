# Accessibility Test Checklist

SwiftDrop should remain usable with touch, keyboard, screen readers, larger text, high contrast, reduced motion, and common platform accessibility settings.

## Screen readers

Test Android TalkBack, iOS VoiceOver, macOS VoiceOver, and Windows Narrator where available.

- Page titles and major sections are announced meaningfully.
- Buttons announce actions instead of visual-only symbols.
- Pairing fingerprint, device name, transfer status, file risk warnings, and receive decisions are understandable without seeing the screen.
- Progress is announced without excessive repeated speech.
- Checkboxes in selective batch approval announce filename and selected state.
- Trusted-device revoke controls identify the relevant device.
- Diagnostic severity is not conveyed only by color.

## Keyboard and focus

- Tab/Shift+Tab reaches all interactive controls on desktop.
- Enter/Space activate focused buttons and checkboxes.
- Focus does not become trapped inside lists or dialogs.
- Modal batch approval returns focus to a logical control when closed.
- Escape/back behaves as reject/cancel where rejecting is the safe default.

## Text scaling and larger interface

- Test system text scaling at several large values.
- Test SwiftDrop's Larger Interface option.
- Long filenames, IPv6 addresses, certificate fingerprints, diagnostics, and translated strings wrap or truncate without hiding required actions.
- Buttons remain at least the configured accessible minimum height.
- No critical text is clipped in portrait, landscape, narrow desktop windows, or split-screen layouts.

## Color and contrast

- Test light and dark themes.
- Test high-contrast/system contrast modes where applicable.
- Risk, error, success, queue, and progress states remain understandable without color.
- Disabled controls remain distinguishable while retaining readable text.

## Motion

- Reduced-motion preference must avoid introducing non-essential animated movement.
- Transfer progress can update numerically/bar-style without flashing or rapid motion.
- No essential information depends on animation timing.

## Language and localization

- Test English resource fallback.
- Test Hindi culture/resource selection.
- Confirm untranslated strings safely fall back rather than displaying resource keys or crashing.
- Verify dates, times, and numeric sizes remain readable under both cultures.

## Touch and motor accessibility

- Primary controls provide adequate target size and spacing.
- Pause, resume, cancel, accept, reject, and trust actions are not placed so closely that accidental activation is likely.
- Destructive actions require confirmation where practical.

## Verification record

For release candidates, record platform/device, OS version, assistive technology/version, SwiftDrop commit, test date, and result. Accessibility cannot be declared fully validated from source review alone; physical assistive-technology testing is required.
