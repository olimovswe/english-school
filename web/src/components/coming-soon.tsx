import { ArrowLeft, type LucideIcon } from "lucide-react";
import Link from "next/link";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardAction, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";

interface ComingSoonProps {
  icon: LucideIcon;
  title: string;
  description: string;
  /** Backend allaqachon nima beradi: sahifa shu imkoniyatlar ustiga quriladi. */
  features: string[];
}

export function ComingSoon({ icon: Icon, title, description, features }: ComingSoonProps) {
  return (
    <div className="grid gap-6">
      <h1 className="font-heading text-2xl font-semibold">{title}</h1>
      <Card className="max-w-xl">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Icon className="size-4 text-muted-foreground" aria-hidden />
            {description}
          </CardTitle>
          <CardDescription>Bu boʻlim keyingi bosqichda qoʻshiladi.</CardDescription>
          <CardAction>
            <Badge variant="secondary">Tez orada</Badge>
          </CardAction>
        </CardHeader>
        <CardContent>
          <ul className="grid list-disc gap-1 pl-5 text-sm text-muted-foreground">
            {features.map((feature) => (
              <li key={feature}>{feature}</li>
            ))}
          </ul>
        </CardContent>
        <CardFooter>
          <Button asChild variant="outline" size="sm">
            <Link href="/dashboard">
              <ArrowLeft aria-hidden />
              Kunlik rejaga qaytish
            </Link>
          </Button>
        </CardFooter>
      </Card>
    </div>
  );
}
