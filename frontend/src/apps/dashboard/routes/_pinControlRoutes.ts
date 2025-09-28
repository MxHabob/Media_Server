import type { AsyncRoute } from 'components/router/AsyncRoute';
import { AppType } from 'constants/appType';

export const ASYNC_PIN_CONTROL_ROUTES: AsyncRoute[] = [
    { path: 'subscriptions', type: AppType.Dashboard },
    { path: 'pins', type: AppType.Dashboard },
    { path: 'pins/report', page: 'pins/report', type: AppType.Dashboard }
];
