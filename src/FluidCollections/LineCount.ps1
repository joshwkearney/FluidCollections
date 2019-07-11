(gci -include *.cs,*.xaml -recurse | select-string .).Count
cmd /c pause | out-null