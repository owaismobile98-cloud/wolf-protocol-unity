@echo off
title WOLF PROTOCOL - Build Characters
echo ============================================================
echo   WOLF PROTOCOL - Build Characters from sprite sheets
echo ============================================================
echo.
echo  1. Drop sprite sheets into:  Assets\Incoming\
echo     named:  name__anim__ColsxRows__fps.png
echo     e.g.    razor__run__8x1__14.png   trooper__attack__6x1__16.png
echo.
echo  IMPORTANT: CLOSE the Unity Editor first - this runs Unity in the
echo  background and cannot run while the editor has the project open.
echo.
echo  Running (headless, ~30-90s on this PC)...
echo ------------------------------------------------------------
"D:\Pro Jack\Games\Unity Hub Editor\6000.5.0f1\Editor\Unity.exe" -batchmode -quit -projectPath "D:\Pro Jack\Games\wolf-protocol-unity" -executeMethod SheetToAnim.BuildIncoming -logFile -
echo ------------------------------------------------------------
if %ERRORLEVEL%==0 (
  echo.
  echo  DONE. Generated characters are in  Assets\Prefabs\
) else (
  echo.
  echo  FAILED ^(exit %ERRORLEVEL%^). Most common cause: the Unity Editor is
  echo  still OPEN. Close it and run this again. Or, with the editor open,
  echo  use the menu:  Window ^> WOLF ^> Build Characters From Incoming
)
echo.
pause
