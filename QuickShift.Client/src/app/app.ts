import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterModule],
  template: `
    <header style="background: white; padding: 3px 20px; box-shadow: 0 2px 8px rgba(0,0,0,0.05); display: flex; align-items: center; justify-content: flex-start; position: sticky; top: 0; z-index: 1000;">
      <img src="/logo.png" alt="QuickShift" style="height: 90px; object-fit: contain; cursor: pointer;" routerLink="/">
    </header>

    <main style="max-width: 1200px; margin: 20px auto; padding: 0 20px;">
      <router-outlet></router-outlet>
    </main>
  `
})
export class App {
  title = 'QuickShift';
}