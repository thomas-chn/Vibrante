   M S B A M L   `   `   `  ????C  ?Vibrante, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null=#clr-namespace:Vibrante.UserControlsVibrante.UserControls  5      )    #Vibrante.UserControls.ComposerTrack   5      R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      5local#clr-namespace:Vibrante.UserControls     5      -   5          >?Name$  userControl??5       ? 	Resources 5
       ?? MergedDictionaries ?? Source?? 5   
    5      ?? 5      $ /Design.xaml??6%   %    5      .??5      ? $??0,5,0,5q?6   $	??140??6   .+?5   
   ?? 
???????$??1,1,1,1q?6%   ? 5   
    ? ColumnDefinitions 5      ?? 5      $??50??6s   $	??300??6?   ??6#   ??  ?? Mode$ OneWay=?   ComposerTrackHeaderWidth ?? Source# [  6?   ?? 5      $
??Auto ?6#   ?? 5      $
??Auto ?6#   ?? 5      $??* ?6#   .+?5      ]? $??0??6   ???	#00000000.+?5      ?? $
??Left=?6   .+?5      ]? .+?5       ?? 
??????   TextForeground#??[ 6;   $??17(?6h   $
??Bold"?6v   $??	New Track??6?   $
??Left=?6?   $??5,3,0,0q?6?   $	??Top=?6?    ?? 	MaxLength$ 20??6?   ??6?   Z? ]? 5!      $	??Top=?6$   $??5,10,5,0q?69   .+?5"      ?? #??[ 6$   $??Sound:??6Q   6d   ?? 5#      -   5         ??Name$ SoundListCB??5#   '   6>   5$      ]? 5%      $	??Top=?6$   $??5,10,5,0q?69   .+?5&      ?? #??[ 6$   $??Interpolation:??6Q   6l   ?? 5'      -   5        $ InterpolationListCB??5'   '   6z   5(      5)      5+      5,      ?? 5.      $??1??6   $??2??6+   $??Right=?65   
???????{?.  6_   ?? 50      -   5        	 ??Name$	 VerticalScaleBar??50      $??2??6/     ButtonBackground#???  6?   }?. $??30??6?   $??
0, 1, 0, 1q?6?   $??Disabled=?6?   

 ? Tag
 6?   ??  ?? ElementName$ userControl??51      ? 53      $??3??6   .+?54      ?? -   5         ??Name$ PositionLabel??54      $??Position: 15ms;168Hz??6/   
???????$??Right=?6a   $??Bottom=?6}   ???	#7F000000$??10(?6?   $??1,1,1,1q?6?    J? Opacity$ 0Q?6?   ?? 55      -   5        $	 SoundCanvas??55      ???	#00000000}?. 
 6?   ?? $ userControl??57      58      5:      5@      5A      