target datalayout = "e-p:32:32:32-i1:8:8-i8:8:8-i16:16:16-i32:32:32-i64:64:64-f32:32:32-f64:64:64-f80:128:128-v64:64:64-v128:128:128-a0:0:64-f80:32:32-n8:16:32-S32"
target triple = "i686-pc-mingw32"

define i32 @main(i32 %argc, i8** nocapture %argv) nounwind {
entry:
  %add = add nsw i32 %argc, 1
  %call = tail call i32 (i8*, ...)* @printf(i8* getelementptr inbounds ([16 x i8]* @.str, i32 0, i32 0), i32 %add) nounwind
  ret i32 0
}

declare i32 @printf(i8* nocapture, ...) nounwind