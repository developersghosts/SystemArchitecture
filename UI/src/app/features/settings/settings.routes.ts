import { Routes } from '@angular/router';
import { unsavedChangesGuard } from '../../core/guards/unsaved-changes.guard';
import { EmailSettingsComponent } from './email-settings/email-settings.component';
import { MessageDictionaryComponent } from './messages/message-dictionary.component';
import { ProjectConfigurationComponent } from './project-configuration/project-configuration.component';

/** Lazy loaded at /settings (Administrator only, see app.routes.ts). */
export const SETTINGS_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'configuration' },
  {
    path: 'configuration',
    component: ProjectConfigurationComponent,
    title: 'nav.configuration',
    canDeactivate: [unsavedChangesGuard]
  },
  { path: 'email', component: EmailSettingsComponent, title: 'nav.email' },
  { path: 'messages', component: MessageDictionaryComponent, title: 'nav.messages' }
];
