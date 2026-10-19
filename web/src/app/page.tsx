import { GraduationCap } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

export default function HomePage() {
  return (
    <main className="flex min-h-svh items-center justify-center p-6">
      <Card className="w-full max-w-md">
        <CardHeader className="gap-3">
          <div className="flex items-center justify-between">
            <GraduationCap className="size-8 text-muted-foreground" aria-hidden />
            <Badge variant="secondary">56 kunlik kurs</Badge>
          </div>
          <CardTitle className="text-2xl">English School</CardTitle>
          <CardDescription>
            Har kuni: ertalabki takrorlash, dars, mashq va talaffuz. Veb ilova tayyorlanmoqda.
          </CardDescription>
        </CardHeader>
      </Card>
    </main>
  );
}
